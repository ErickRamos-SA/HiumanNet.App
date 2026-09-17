namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Lista las incidencias de un período, una fila por contrato vigente.
/// </summary>
/// <param name="PeriodoId">Período consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarIncidenciasQuery(Guid PeriodoId, Guid? EmpresaId);
