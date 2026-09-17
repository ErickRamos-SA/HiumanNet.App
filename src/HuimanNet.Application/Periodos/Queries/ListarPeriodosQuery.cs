namespace HuimanNet.Application.Periodos.Queries;

/// <summary>
/// Consulta los períodos de una empresa.
/// </summary>
/// <param name="EmpresaId">
/// Empresa objetivo. Sólo la aportan los roles transversales; para la empresa
/// cliente se impone la del token.
/// </param>
/// <param name="IncluirCerrados">Si es <c>false</c>, omite los períodos cerrados.</param>
public sealed record ListarPeriodosQuery(Guid? EmpresaId, bool IncluirCerrados);
