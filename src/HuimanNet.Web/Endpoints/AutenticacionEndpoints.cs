using System.Security.Claims;
using HuimanNet.Application.Common;
using HuimanNet.Application.Usuarios;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace HuimanNet.Web.Endpoints;

/// <summary>
/// Rutas de inicio y cierre de sesión del portal.
/// </summary>
/// <remarks>
/// En modo local el formulario de <see cref="RutaDeEntrada"/> publica aquí el
/// correo y la contraseña; los valida el caso de uso de sesión
/// (<see cref="ValidarCredencialesLocalesCommand"/>, el mismo manejador que
/// emite el token de la app móvil) y, si son correctos, se emite la cookie de
/// sesión. En modo Entra se delega en OpenID Connect. Esta clase no accede a
/// datos: sólo traduce HTTP y la cookie.
/// </remarks>
public static class AutenticacionEndpoints
{
    /// <summary>Página de inicio de sesión.</summary>
    public const string RutaDeEntrada = "/entrar";

    /// <summary>Ruta que cierra la sesión.</summary>
    public const string RutaDeSalida = "/autenticacion/salir";

    /// <summary>Nombre de la política de limitación de intentos de inicio de sesión.</summary>
    public const string PoliticaDeInicioDeSesion = "inicio-de-sesion";

    /// <summary>
    /// Mapea las rutas de autenticación según el modo de identidad.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <param name="modoLocal"><c>true</c> para identidad local; <c>false</c> para Entra.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearAutenticacion(this IEndpointRouteBuilder app, bool modoLocal)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (modoLocal)
        {
            app.MapPost("/autenticacion/local", IniciarSesionLocalAsync)
                .AllowAnonymous()
                .RequireRateLimiting(PoliticaDeInicioDeSesion);

            app.MapGet("/autenticacion/entrar", (string? volverA) =>
                    TypedResults.LocalRedirect(RutaDeEntrada + "?volverA=" + Uri.EscapeDataString(RutaLocalSegura(volverA))))
                .AllowAnonymous();

            app.MapGet(RutaDeSalida, async (HttpContext contexto) =>
                {
                    await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return TypedResults.LocalRedirect(RutaDeEntrada);
                })
                .AllowAnonymous();
        }
        else
        {
            app.MapGet("/autenticacion/entrar", (string? volverA) =>
                    TypedResults.Challenge(new AuthenticationProperties { RedirectUri = RutaLocalSegura(volverA) }))
                .AllowAnonymous();

            app.MapGet(RutaDeSalida, () =>
                    TypedResults.SignOut(
                        new AuthenticationProperties { RedirectUri = "/" },
                        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
                .AllowAnonymous();
        }

        app.MapGet("/autenticacion/idioma", (string codigo, string? volverA, HttpContext contexto) =>
            {
                IdiomaDeLaPeticion.Recordar(contexto, IdiomaExtensiones.DesdeCodigo(codigo));
                return TypedResults.LocalRedirect(RutaLocalSegura(volverA));
            })
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Devuelve la ruta si es local al sitio; la raíz en otro caso.
    /// </summary>
    /// <param name="ruta">Ruta propuesta por el cliente.</param>
    /// <returns>Una ruta local segura (evita redirecciones abiertas).</returns>
    public static string RutaLocalSegura(string? ruta)
        => ruta is { Length: > 0 } && ruta[0] == '/' && !ruta.StartsWith("//", StringComparison.Ordinal)
           && !ruta.StartsWith("/\\", StringComparison.Ordinal)
            ? ruta
            : "/";

    /// <summary>
    /// Valida el formulario de acceso local y, si las credenciales son correctas,
    /// emite la cookie de sesión.
    /// </summary>
    /// <param name="correo">Correo escrito en el formulario.</param>
    /// <param name="contrasena">Contraseña escrita en el formulario.</param>
    /// <param name="volverA">Página a la que volver tras entrar.</param>
    /// <param name="contexto">Contexto HTTP de la petición.</param>
    /// <param name="manejador">Caso de uso que valida las credenciales.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Una redirección a la página pedida (o al perfil si debe cambiar la
    /// contraseña); de vuelta al acceso con error si las credenciales no valen.
    /// </returns>
    private static async Task<IResult> IniciarSesionLocalAsync(
        [FromForm] string correo,
        [FromForm] string contrasena,
        [FromForm] string? volverA,
        HttpContext contexto,
        IManejadorDeComando<ValidarCredencialesLocalesCommand, SesionLocalValidada> manejador,
        CancellationToken cancellationToken)
    {
        string destino = RutaLocalSegura(volverA);

        try
        {
            SesionLocalValidada usuario = await manejador.EjecutarAsync(
                new ValidarCredencialesLocalesCommand(correo, contrasena, contexto.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            // La cookie sólo identifica: rol, empresa y permisos se leen de la
            // base de datos en cada circuito.
            var identidad = new ClaimsIdentity(
                [
                    new Claim("sub", usuario.IdentificadorExterno),
                    new Claim("name", usuario.NombreCompleto),
                    new Claim("preferred_username", usuario.Correo),
                    new Claim("roles", usuario.Rol.ToString()),
                ],
                authenticationType: "local",
                nameType: "name",
                roleType: "roles");

            await contexto.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad),
                new AuthenticationProperties { IsPersistent = false });

            IdiomaDeLaPeticion.Recordar(contexto, usuario.Idioma);

            return TypedResults.LocalRedirect(usuario.RequiereCambioDeContrasena ? "/perfil?cambiarContrasena=1" : destino);
        }
        catch (AccesoNoAutorizadoException)
        {
            return TypedResults.LocalRedirect($"{RutaDeEntrada}?error=1&volverA={Uri.EscapeDataString(destino)}");
        }
    }
}
