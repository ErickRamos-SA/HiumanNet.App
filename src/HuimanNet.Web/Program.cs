using System.Threading.RateLimiting;
using HuimanNet.Application;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Infrastructure;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Storage;
using HuimanNet.Web.Components;
using HuimanNet.Web.Configuracion;
using HuimanNet.Web.Endpoints;
using HuimanNet.Web.Notificaciones;
using HuimanNet.Web.Seguridad;
using HuimanNet.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Registro estructurado. A diferencia de la API (AOT), la web se compila JIT y
// puede usar Serilog sin restricciones de recorte.
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((contexto, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------------------------------------------------------------------------
// Capas de la solución. La web consume los mismos casos de uso que la API, pero
// EN PROCESO: no hay salto HTTP y las reglas de negocio no se duplican.
// ---------------------------------------------------------------------------
builder.Services.AgregarCapaDeAplicacion();
builder.Services.AgregarCapaDeInfraestructura(builder.Configuration);

builder.Services.Configure<OpcionesDeCorreo>(builder.Configuration.GetSection(OpcionesDeCorreo.Seccion));

// ---------------------------------------------------------------------------
// Identidad: sesión en cookie. En modo Entra se inicia con OpenID Connect; en
// modo local, con el formulario propio validado por el caso de uso de sesión.
// ---------------------------------------------------------------------------
OpcionesDeIdentidad identidad = builder.Configuration
    .GetSection(OpcionesDeIdentidad.Seccion)
    .Get<OpcionesDeIdentidad>() ?? new OpcionesDeIdentidad();

OpcionesDeEntraWeb entra = builder.Configuration
    .GetSection(OpcionesDeEntraWeb.Seccion)
    .Get<OpcionesDeEntraWeb>() ?? new OpcionesDeEntraWeb();

AuthenticationBuilder autenticacion = builder.Services
    .AddAuthentication(opciones =>
    {
        opciones.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opciones.DefaultChallengeScheme = identidad.EsLocal
            ? CookieAuthenticationDefaults.AuthenticationScheme
            : OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = "huimannet.sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.SlidingExpiration = true;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.LoginPath = AutenticacionEndpoints.RutaDeEntrada;
        opciones.LogoutPath = AutenticacionEndpoints.RutaDeSalida;
        opciones.ReturnUrlParameter = "volverA";
    });

if (!identidad.EsLocal)
{
    autenticacion.AddOpenIdConnect(opciones =>
    {
        opciones.Authority = entra.Authority;
        opciones.ClientId = entra.ClientId;
        opciones.ClientSecret = entra.ClientSecret;
        opciones.CallbackPath = entra.CallbackPath;
        opciones.SignedOutCallbackPath = entra.SignedOutCallbackPath;
        opciones.RequireHttpsMetadata = entra.RequireHttpsMetadata;
        opciones.ResponseType = "code";
        opciones.UsePkce = true;
        opciones.SaveTokens = false;
        opciones.GetClaimsFromUserInfoEndpoint = true;
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters.RoleClaimType = "roles";
        opciones.TokenValidationParameters.NameClaimType = "name";
        opciones.Scope.Add("openid");
        opciones.Scope.Add("profile");
        opciones.Scope.Add("email");
    });
}

// Todo el portal exige autenticación: la política de respaldo evita que una
// página nueva quede accidentalmente abierta al público.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddCascadingAuthenticationState();

// Freno a la fuerza bruta en el formulario de acceso.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy(AutenticacionEndpoints.PoliticaDeInicioDeSesion, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        static _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Estado del circuito: identidad efectiva, idioma y empresa de trabajo.
builder.Services.AddScoped(_ => new Traductor());
builder.Services.AddScoped<EstadoDelPortal>();
builder.Services.AddScoped<ContextoDeUsuarioDelCircuito>();
builder.Services.AddScoped<PortadorDeUsuarioActual>();
builder.Services.AddScoped<IUsuarioActual>(sp =>
    sp.GetRequiredService<PortadorDeUsuarioActual>().Usuario ?? sp.GetRequiredService<ContextoDeUsuarioDelCircuito>());
builder.Services.AddScoped<EjecutorDeCasosDeUso>();

// ---------------------------------------------------------------------------
// Trabajador de avisos por correo.
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue<bool>($"{OpcionesDeCorreo.Seccion}:Habilitado"))
{
    builder.Services.AddSingleton<IEnviadorDeCorreo, EnviadorDeCorreoAcs>();
    builder.Services.AddHostedService<TrabajadorDeAvisos>();
}

// ---------------------------------------------------------------------------
// Componentes de Blazor Server.
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opciones => opciones.DetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// Cada paso de preparación se habilita por configuración (en Azure sólo se
// siembran los datos iniciales que falten).
await PreparacionDelEntorno.EjecutarAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapHealthChecks("/salud").AllowAnonymous();
app.MapearAutenticacion(identidad.EsLocal);

if (app.Services.GetService<ServicioDeAlmacenLocal>() is not null)
{
    app.MapearAlmacenLocal();
}

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
