namespace HuimanNet.Application.Nomina;

/// <summary>
/// Lista las corridas de un período.
/// </summary>
/// <param name="PeriodoId">Período consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarCorridasQuery(Guid PeriodoId, Guid? EmpresaId);
