namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Importa las incidencias de un período desde uno de sus archivos de incidencias.
/// </summary>
/// <param name="DocumentoId">Documento de tipo incidencias del período, ya disponible.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <remarks>
/// El archivo no viaja en la petición: se toma del período, adonde llegó por el
/// flujo de carga con análisis antimalware. Así todo intercambio de archivos
/// queda asociado a un período y registrado en la bitácora.
/// </remarks>
public sealed record ImportarIncidenciasCommand(Guid DocumentoId, Guid? EmpresaId);
