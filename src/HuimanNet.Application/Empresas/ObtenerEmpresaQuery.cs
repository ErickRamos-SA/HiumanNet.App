namespace HuimanNet.Application.Empresas;

/// <summary>
/// Obtiene el detalle de una empresa.
/// </summary>
/// <param name="EmpresaId">Empresa consultada, o <c>null</c> para la del solicitante.</param>
public sealed record ObtenerEmpresaQuery(Guid? EmpresaId);
