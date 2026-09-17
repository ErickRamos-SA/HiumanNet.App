using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Cambia el estado de una corrida (aprobar o descartar).
/// </summary>
/// <param name="CorridaId">Corrida afectada.</param>
/// <param name="Estado">Estado destino.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CambiarEstadoCorridaCommand(Guid CorridaId, EstadoDeCorrida Estado, Guid? EmpresaId, string? Observaciones);
