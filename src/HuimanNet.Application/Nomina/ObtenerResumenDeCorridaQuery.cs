namespace HuimanNet.Application.Nomina;

/// <summary>
/// Obtiene una corrida con sus resultados y la facturación estimada.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerResumenDeCorridaQuery(Guid CorridaId, Guid? EmpresaId);
