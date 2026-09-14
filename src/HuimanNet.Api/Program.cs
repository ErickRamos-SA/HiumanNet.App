using System.Threading.RateLimiting;
using HuimanNet.Api.Configuracion;
using HuimanNet.Api.Endpoints;
using HuimanNet.Api.Extensions;
using HuimanNet.Api.Seguridad;
using HuimanNet.Application;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Serialization;
using HuimanNet.Infrastructure;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Identity;
using HuimanNet.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// CreateSlimBuilder: el anfitrión mínimo pensado para Native AOT. Evita el
// registro de servicios que la API no usa (ESPECIFICACION.md §6).
WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

// El anfitrión mínimo no incluye HTTPS: se habilita explícitamente.
builder.WebHost.UseKestrelHttpsConfiguration();

// ---------------------------------------------------------------------------
// Serialización: metadatos generados en compilación, sin reflexión.
// ---------------------------------------------------------------------------
builder.Services.ConfigureHttpJsonOptions(opciones =>
{
    opciones.SerializerOptions.TypeInfoResolverChain.Insert(0, HuimanNetJsonContext.Default);
});

// ---------------------------------------------------------------------------
// Configuración y capas de la solución.
// ---------------------------------------------------------------------------
builder.Services.Configure<OpcionesDeEntra>(builder.Configuration.GetSection(OpcionesDeEntra.Seccion));

builder.Services.AgregarCapaDeAplicacion();
builder.Services.AgregarCapaDeInfraestructura(builder.Configuration);

// ---------------------------------------------------------------------------
// Identidad del solicitante: la base de datos es la fuente de verdad del rol,
// la empresa y los permisos (ver MiddlewareDeUsuarioActual).
// ---------------------------------------------------------------------------
builder.Services.AddScoped<UsuarioActualDeHttpContext>();
builder.Services.AddScoped<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioActualDeHttpContext>());

// ---------------------------------------------------------------------------
// Autenticación: token de Microsoft Entra o token local (HS256) según
// 'Identidad:Modo'. En ambos casos el esquema es JwtBearer y el resto de la
// canalización es idéntica.
// ---------------------------------------------------------------------------
OpcionesDeIdentidad identidad = builder.Configuration
    .GetSection(OpcionesDeIdentidad.Seccion)
    .Get<OpcionesDeIdentidad>() ?? new OpcionesDeIdentidad();

OpcionesDeEntra entra = builder.Configuration
    .GetSection(OpcionesDeEntra.Seccion)
    .Get<OpcionesDeEntra>() ?? new OpcionesDeEntra();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters.ClockSkew = entra.ToleranciaDeReloj;
        opciones.TokenValidationParameters.ValidateIssuer = true;
        opciones.TokenValidationParameters.ValidateAudience = true;
        opciones.TokenValidationParameters.ValidateLifetime = true;
        opciones.TokenValidationParameters.RoleClaimType = "roles";
        opciones.TokenValidationParameters.NameClaimType = "name";

        if (identidad.EsLocal)
        {
            opciones.TokenValidationParameters.ValidIssuer = identidad.EmisorLocal;
            opciones.TokenValidationParameters.ValidAudience = identidad.AudienciaLocal;
            opciones.TokenValidationParameters.ValidateIssuerSigningKey = true;
            opciones.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
        }
        else
        {
            opciones.Authority = entra.Autoridad;
            opciones.Audience = entra.Audiencia;
            opciones.RequireHttpsMetadata = entra.ExigirHttpsEnMetadatos;
        }
    });

if (identidad.EsLocal)
{
    // La clave de firma vive en un singleton compartido con el emisor de tokens.
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<MaterialDeFirmaLocal>((opciones, material) =>
            opciones.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(material.Clave));
}

builder.Services.AddAuthorizationBuilder().AgregarPoliticasDeHuimanNet();

// Freno a la fuerza bruta: pocos intentos de inicio de sesión por IP y minuto.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy(SesionEndpoints.PoliticaDeInicioDeSesion, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        static _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// ---------------------------------------------------------------------------
// Errores como ProblemDetails (RFC 7807) y documentación OpenAPI.
// ---------------------------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorGlobalDeExcepciones>();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// ---------------------------------------------------------------------------
// Preparación del entorno: cada paso se habilita por configuración (en Azure
// sólo se siembran los datos iniciales que falten).
// ---------------------------------------------------------------------------
await PreparacionDelEntorno.EjecutarAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // En desarrollo se admite HTTP para que el emulador de Android llegue sin certificado.
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UsarUsuarioActual();
app.UseRateLimiter();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Endpoints.
// ---------------------------------------------------------------------------
app.MapHealthChecks(RutasApi.Salud).AllowAnonymous();

app.MapearEndpointsDeSesion();
app.MapearEndpointsDeDocumentos();
app.MapearDocumentosDePeriodo();
app.MapearEndpointsDePeriodos();
app.MapearEndpointsDeEmpresas();
app.MapearEndpointsDeEmpleados();
app.MapearEndpointsDeNomina();
app.MapearEndpointsDeCatalogos();
app.MapearEndpointsDeAuditoria();

if (app.Services.GetService<ServicioDeAlmacenLocal>() is not null)
{
    app.MapearAlmacenLocal();
}

await app.RunAsync();

/// <summary>
/// Punto de entrada de la API; parcial para exponerlo a las pruebas de integración.
/// </summary>
public partial class Program;
