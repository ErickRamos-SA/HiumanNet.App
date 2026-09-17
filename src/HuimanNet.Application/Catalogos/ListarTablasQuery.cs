namespace HuimanNet.Application.Catalogos;

/// <summary>Lista las tablas globales y las de una empresa.</summary>
/// <param name="EmpresaId">Empresa, o <c>null</c> para sólo globales.</param>
public sealed record ListarTablasQuery(Guid? EmpresaId);
