using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Petición para aprobar o descartar una corrida.
/// </summary>
/// <param name="Estado">Estado destino: aprobada o descartada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CambiarEstadoCorridaRequest(EstadoDeCorrida Estado, Guid? EmpresaId, string? Observaciones = null);
