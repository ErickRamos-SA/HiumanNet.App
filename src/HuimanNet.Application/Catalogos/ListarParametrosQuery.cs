namespace HuimanNet.Application.Catalogos;

/// <summary>Lista los parámetros globales y los de una empresa.</summary>
/// <param name="EmpresaId">Empresa, o <c>null</c> para sólo globales.</param>
public sealed record ListarParametrosQuery(Guid? EmpresaId);
