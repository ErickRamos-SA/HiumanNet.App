using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Derivación y verificación de <i>hashes</i> de contraseña para el modo de
/// identidad local.
/// </summary>
/// <remarks>
/// La contraseña en claro nunca se almacena ni se registra. La implementación
/// usa una función de derivación con sal y factor de trabajo configurable.
/// </remarks>
public interface IHasherDeContrasenas
{
    /// <summary>
    /// Deriva el <i>hash</i> de una contraseña.
    /// </summary>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <returns>Cadena autocontenida con algoritmo, sal e iteraciones.</returns>
    string Hashear(string contrasena);

    /// <summary>
    /// Comprueba una contraseña contra un <i>hash</i>.
    /// </summary>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <param name="hash">Hash almacenado.</param>
    /// <returns><c>true</c> si coinciden.</returns>
    bool Verificar(string contrasena, string hash);
}

/// <summary>
/// Token de acceso emitido para un usuario local.
/// </summary>
/// <param name="Token">Token compacto.</param>
/// <param name="ExpiraEn">Instante de caducidad, en UTC.</param>
public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);

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

/// <summary>
/// Reglas de calidad de contraseña.
/// </summary>
public static class PoliticaDeContrasenas
{
    /// <summary>Longitud mínima aceptada.</summary>
    public const int LongitudMinima = 8;

    /// <summary>
    /// Valida una contraseña.
    /// </summary>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <returns>Mensaje de error, o <c>null</c> si es aceptable.</returns>
    public static string? Validar(string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < LongitudMinima)
        {
            return $"La contraseña debe tener al menos {LongitudMinima} caracteres.";
        }

        bool tieneLetra = contrasena.Any(char.IsLetter);
        bool tieneDigito = contrasena.Any(char.IsDigit);

        return tieneLetra && tieneDigito ? null : "La contraseña debe combinar letras y números.";
    }
}
