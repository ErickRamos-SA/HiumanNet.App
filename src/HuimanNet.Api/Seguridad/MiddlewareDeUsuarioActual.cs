using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Infrastructure.Identity;

namespace HuimanNet.Api.Seguridad;

/// <summary>
/// Traduce la identidad autenticada de la petición a un usuario de HuimanNet y
/// la deja disponible para los casos de uso.
/// </summary>
/// <remarks>
/// Se ejecuta entre la autenticación y la autorización, de modo que las
/// políticas pueden decidir con el rol vigente en la base de datos. Un token
/// válido de un usuario desactivado o no registrado recibe <c>401</c>: el
/// cliente debe volver a iniciar sesión.
/// </remarks>
public sealed class MiddlewareDeUsuarioActual
{
    private readonly RequestDelegate _siguiente;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="MiddlewareDeUsuarioActual"/>.
    /// </summary>
    /// <param name="siguiente">Siguiente componente de la canalización.</param>
    public MiddlewareDeUsuarioActual(RequestDelegate siguiente) => _siguiente = siguiente;

    /// <summary>
    /// Resuelve el usuario de la petición, si está autenticada.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="resolutor">Resolutor de usuarios por <i>claims</i>.</param>
    /// <param name="usuarioActual">Contenedor del usuario de la petición.</param>
    /// <param name="logger">Registro de eventos.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        ResolutorDeUsuarioPorClaims resolutor,
        UsuarioActualDeHttpContext usuarioActual,
        ILogger<MiddlewareDeUsuarioActual> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                Usuario usuario = await resolutor.ResolverAsync(context.User, context.RequestAborted);
                usuarioActual.Establecer(usuario, context.Connection.RemoteIpAddress?.ToString());
            }
            catch (AccesoNoAutorizadoException excepcion)
            {
                logger.LogWarning("Token válido rechazado: {Motivo}", excepcion.Message);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _siguiente(context);
    }
}

/// <summary>
/// Extensiones de registro de <see cref="MiddlewareDeUsuarioActual"/>.
/// </summary>
public static class MiddlewareDeUsuarioActualExtensions
{
    /// <summary>
    /// Agrega la resolución del usuario actual a la canalización.
    /// </summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IApplicationBuilder UsarUsuarioActual(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<MiddlewareDeUsuarioActual>();
    }
}
