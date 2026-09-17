using System.Text.Json.Serialization;

namespace HuimanNet.Web.Services;

/// <summary>
/// Metadatos del archivo que el usuario seleccionó en el navegador.
/// </summary>
/// <param name="Nombre">Nombre original del archivo.</param>
/// <param name="Tamano">Tamaño del archivo en bytes.</param>
public sealed record ArchivoSeleccionado(
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("tamano")] long Tamano);
