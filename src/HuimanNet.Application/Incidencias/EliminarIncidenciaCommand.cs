namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Elimina la incidencia capturada de un contrato en un período.
/// </summary>
/// <param name="IncidenciaId">Incidencia a eliminar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record EliminarIncidenciaCommand(Guid IncidenciaId, Guid? EmpresaId);
