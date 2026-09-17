using HuimanNet.Application.Usuarios;
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
    /// <param name="lector">Lector de la identidad en los <i>claims</i>.</param>
    /// <param name="resolutor">Caso de uso que traduce la identidad a un usuario del portal.</param>
    /// <param name="usuarioActual">Contenedor del usuario de la petición.</param>
    /// <param name="logger">Registro de eventos.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        LectorDeIdentidadPorClaims lector,
        ResolutorDeUsuarioAutenticado resolutor,
        UsuarioActualDeHttpContext usuarioActual,
        ILogger<MiddlewareDeUsuarioActual> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(lector);
        ArgumentNullException.ThrowIfNull(resolutor);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                string? direccionIp = context.Connection.RemoteIpAddress?.ToString();
                Usuario usuario = await resolutor.ResolverAsync(lector.Leer(context.User), direccionIp, context.RequestAborted);
                usuarioActual.Establecer(usuario, direccionIp);
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
