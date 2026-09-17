namespace HuimanNet.Application.Documentos.Queries;

/// <summary>
/// Solicita el enlace temporal de descarga de un documento.
/// </summary>
/// <param name="DocumentoId">Documento que se desea descargar.</param>
public sealed record ObtenerEnlaceDescargaQuery(Guid DocumentoId);
