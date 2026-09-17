namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Petición para calcular la nómina de un período.
/// </summary>
/// <param name="PeriodoId">Período a calcular.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CalcularNominaRequest(Guid PeriodoId, Guid? EmpresaId, string? Observaciones = null);
