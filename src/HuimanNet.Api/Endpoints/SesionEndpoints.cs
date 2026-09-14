using System.Reflection;
using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Inicio;
using HuimanNet.Application.Usuarios;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints de identidad: configuración pública, inicio de sesión local,
/// perfil del usuario y panel de inicio.
/// </summary>
public static class SesionEndpoints
{
    /// <summary>Nombre de la política de limitación de intentos de inicio de sesión.</summary>
    public const string PoliticaDeInicioDeSesion = "inicio-de-sesion";

    private static readonly string Version =
        typeof(SesionEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "1.0.0";

    /// <summary>
    /// Mapea los endpoints de sesión.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeSesion(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(RutasApi.Configuracion, ObtenerConfiguracion)
            .WithTags("Identidad")
            .WithName("ObtenerConfiguracionPublica")
            .WithSummary("Indica al cliente cómo autenticarse (Entra o local).")
            .AllowAnonymous();

        app.MapPost(RutasApi.IniciarSesion, IniciarSesionAsync)
            .WithTags("Identidad")
            .WithName("IniciarSesionLocal")
            .WithSummary("Valida correo y contraseña y emite un token de acceso (modo local).")
            .AllowAnonymous()
            .RequireRateLimiting(PoliticaDeInicioDeSesion)
            .Produces<IniciarSesionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        RouteGroupBuilder perfil = app.MapGroup(RutasApi.UsuarioActual)
            .WithTags("Identidad")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        perfil.MapGet("/", ObtenerUsuarioActualAsync)
            .WithName("ObtenerUsuarioActual")
            .WithSummary("Devuelve la identidad efectiva, el idioma y las acciones permitidas.");

        perfil.MapPut("/contrasena", CambiarContrasenaAsync)
            .WithName("CambiarContrasena")
            .WithSummary("Cambia la contraseña local del usuario.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        perfil.MapPut("/preferencias", ActualizarPreferenciasAsync)
            .WithName("ActualizarPreferencias")
            .WithSummary("Cambia el idioma del usuario.");

        app.MapGet(RutasApi.Inicio, ObtenerResumenDeInicioAsync)
            .WithTags("Inicio")
            .WithName("ObtenerResumenDeInicio")
            .WithSummary("Indicadores y tareas pendientes del panel de inicio.")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        return app;
    }

    private static Ok<ConfiguracionPublicaDto> ObtenerConfiguracion(
        IOptions<OpcionesDeIdentidad> identidad, IOptions<OpcionesDeAlmacenamiento> almacenamiento)
        => TypedResults.Ok(new ConfiguracionPublicaDto(
            identidad.Value.EsLocal ? OpcionesDeIdentidad.ModoLocal : OpcionesDeIdentidad.ModoEntra,
            Version,
            almacenamiento.Value.EsLocal ? OpcionesDeAlmacenamiento.ProveedorLocal : OpcionesDeAlmacenamiento.ProveedorAzureBlob));

    private static async Task<IResult> IniciarSesionAsync(
        IniciarSesionRequest peticion,
        HttpContext contexto,
        IOptions<OpcionesDeIdentidad> identidad,
        IManejadorDeComando<IniciarSesionLocalCommand, IniciarSesionResponse> manejador,
        CancellationToken cancellationToken)
    {
        if (!identidad.Value.EsLocal)
        {
            return TypedResults.NotFound();
        }

        try
        {
            IniciarSesionResponse respuesta = await manejador.EjecutarAsync(
                new IniciarSesionLocalCommand(peticion.Correo, peticion.Contrasena, contexto.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            return TypedResults.Ok(respuesta);
        }
        catch (AccesoNoAutorizadoException)
        {
            // Mensaje único para correo inexistente, contraseña errónea o cuenta
            // desactivada: no revela cuál de los tres ocurrió.
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales no válidas",
                detail: "El correo o la contraseña no son correctos.");
        }
    }

    private static async Task<Ok<UsuarioActualDto>> ObtenerUsuarioActualAsync(
        IManejadorDeConsulta<ObtenerUsuarioActualQuery, UsuarioActualDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerUsuarioActualQuery(), cancellationToken));

    private static async Task<NoContent> CambiarContrasenaAsync(
        CambiarContrasenaRequest peticion,
        IManejadorDeComando<CambiarContrasenaCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(
            new CambiarContrasenaCommand(peticion.ContrasenaActual, peticion.NuevaContrasena), cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ActualizarPreferenciasAsync(
        ActualizarPreferenciasRequest peticion,
        IManejadorDeComando<ActualizarPreferenciasCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new ActualizarPreferenciasCommand(peticion.Idioma), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ResumenDeInicioDto>> ObtenerResumenDeInicioAsync(
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerResumenDeInicioQuery, ResumenDeInicioDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerResumenDeInicioQuery(empresaId), cancellationToken));
}
