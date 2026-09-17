using System.Text.Json.Serialization;

namespace HuimanNet.Web.Services;

/// <summary>
/// Resultado que devuelve el módulo JavaScript de carga directa.
/// </summary>
/// <param name="Exito">Indica si el archivo llegó completo al almacenamiento.</param>
/// <param name="Huella">Hash SHA-256 hexadecimal calculado en el navegador.</param>
/// <param name="Tamano">Tamaño real del archivo subido, en bytes.</param>
/// <param name="Mensaje">Descripción del fallo cuando <paramref name="Exito"/> es <c>false</c>.</param>
public sealed record ResultadoDeCargaDirecta(
    [property: JsonPropertyName("exito")] bool Exito,
    [property: JsonPropertyName("huella")] string? Huella,
    [property: JsonPropertyName("tamano")] long Tamano,
    [property: JsonPropertyName("mensaje")] string? Mensaje);
