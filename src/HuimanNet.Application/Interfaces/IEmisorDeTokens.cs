using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Emisión de tokens de acceso para el modo de identidad local.
/// </summary>
/// <remarks>
/// Lo usa la API para que la app móvil pueda autenticarse sin Microsoft Entra.
/// El token lleva el identificador del usuario y su rol; el servidor vuelve a
/// resolver la identidad local en cada petición, de modo que un cambio de rol
/// o una desactivación surten efecto de inmediato.
/// </remarks>
public interface IEmisorDeTokens
{
    /// <summary>
    /// Emite un token para un usuario.
    /// </summary>
    /// <param name="usuario">Usuario autenticado.</param>
    /// <returns>El token y su caducidad.</returns>
    TokenEmitido Emitir(Usuario usuario);
}
