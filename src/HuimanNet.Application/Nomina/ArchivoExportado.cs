namespace HuimanNet.Application.Nomina;

/// <summary>
/// Archivo generado para descarga.
/// </summary>
/// <param name="Nombre">Nombre sugerido.</param>
/// <param name="TipoDeContenido">Tipo MIME.</param>
/// <param name="Contenido">Bytes del archivo.</param>
public sealed record ArchivoExportado(string Nombre, string TipoDeContenido, byte[] Contenido);
