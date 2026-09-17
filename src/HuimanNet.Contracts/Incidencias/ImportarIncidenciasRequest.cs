namespace HuimanNet.Contracts.Incidencias;

/// <summary>
/// Petición para importar las incidencias de un período desde uno de sus
/// archivos de incidencias (CSV o XLSX con el diseño de la hoja de incidencias
/// del modelo de referencia).
/// </summary>
/// <param name="DocumentoId">Documento de tipo incidencias del período, ya disponible.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <remarks>
/// El archivo no viaja en la petición: se sube antes a los documentos del
/// período y aquí sólo se indica cuál importar.
/// </remarks>
public sealed record ImportarIncidenciasRequest(Guid DocumentoId, Guid? EmpresaId);
