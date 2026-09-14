namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Respuesta con la URL SAS de lectura para descargar un documento.
/// </summary>
/// <param name="DocumentoId">Identificador del documento solicitado.</param>
/// <param name="UrlDescarga">URL SAS de lectura, de un solo blob y corta vigencia.</param>
/// <param name="NombreArchivo">Nombre con el que debe guardarse el archivo descargado.</param>
/// <param name="TamanoBytes">Tamaño del archivo en bytes, para mostrar progreso.</param>
/// <param name="ExpiraEn">Instante en que caduca la URL, en UTC.</param>
/// <remarks>
/// La emisión de esta URL queda registrada en la bitácora de auditoría: saber
/// quién descargó es un requisito de cumplimiento (ARQUITECTURA.md §6.3).
/// </remarks>
public sealed record EnlaceDescargaResponse(
    Guid DocumentoId,
    Uri UrlDescarga,
    string NombreArchivo,
    long TamanoBytes,
    DateTimeOffset ExpiraEn);
