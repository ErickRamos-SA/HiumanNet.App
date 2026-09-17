namespace HuimanNet.Application.Nomina;

/// <summary>
/// Calcula la nómina de un período con el motor del sistema.
/// </summary>
/// <param name="PeriodoId">Período a calcular.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CalcularNominaCommand(Guid PeriodoId, Guid? EmpresaId, string? Observaciones);
