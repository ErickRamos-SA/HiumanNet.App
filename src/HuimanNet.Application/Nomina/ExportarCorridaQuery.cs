namespace HuimanNet.Application.Nomina;

/// <summary>
/// Exporta una corrida a CSV.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ExportarCorridaQuery(Guid CorridaId, Guid? EmpresaId);
