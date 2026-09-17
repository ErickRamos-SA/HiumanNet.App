namespace HuimanNet.Domain.Enums;

/// <summary>
/// Utilidades del enumerado <see cref="Idioma"/>.
/// </summary>
public static class IdiomaExtensiones
{
    /// <summary>
    /// Devuelve el código de cultura de dos letras del idioma.
    /// </summary>
    /// <param name="idioma">Idioma a convertir.</param>
    /// <returns><c>"es"</c> o <c>"en"</c>.</returns>
    public static string Codigo(this Idioma idioma) => idioma == Idioma.Ingles ? "en" : "es";

    /// <summary>
    /// Interpreta un código de cultura.
    /// </summary>
    /// <param name="codigo">Código como <c>"es"</c>, <c>"en"</c> o <c>"en-US"</c>.</param>
    /// <returns>El idioma correspondiente; español si el código no se reconoce.</returns>
    public static Idioma DesdeCodigo(string? codigo)
        => codigo is not null && codigo.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? Idioma.Ingles
            : Idioma.Espanol;
}
