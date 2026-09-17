namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Propiedades observadas de un blob ya almacenado.
/// </summary>
/// <param name="TamanoBytes">Tamaño real del contenido, en bytes.</param>
/// <param name="HashMd5Base64">
/// Hash MD5 que calcula el servicio de almacenamiento, en Base64, o <c>null</c>
/// si no está disponible.
/// </param>
/// <param name="UltimaModificacion">Instante de la última escritura, en UTC.</param>
public sealed record PropiedadesDeBlob(
    long TamanoBytes,
    string? HashMd5Base64,
    DateTimeOffset UltimaModificacion);
