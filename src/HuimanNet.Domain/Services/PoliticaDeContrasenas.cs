namespace HuimanNet.Domain.Services;

/// <summary>
/// Reglas de calidad de las contraseñas locales.
/// </summary>
/// <remarks>
/// Regla de negocio pura sobre las credenciales de <see cref="Entities.Usuario"/>:
/// la aplican por igual el alta, el restablecimiento y el cambio de contraseña,
/// sin importar desde qué cliente se pidan.
/// </remarks>
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
