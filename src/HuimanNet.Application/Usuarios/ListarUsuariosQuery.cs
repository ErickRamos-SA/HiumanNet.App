namespace HuimanNet.Application.Usuarios;

/// <summary>Lista los usuarios.</summary>
/// <param name="EmpresaId">Empresa a filtrar, o <c>null</c>.</param>
/// <param name="IncluirInactivos">Si se incluyen los desactivados.</param>
public sealed record ListarUsuariosQuery(Guid? EmpresaId, bool IncluirInactivos);
