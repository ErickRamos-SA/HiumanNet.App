namespace HuimanNet.Application.Usuarios;

/// <summary>Obtiene un usuario para la administración.</summary>
/// <param name="UsuarioId">Usuario consultado.</param>
public sealed record ObtenerUsuarioQuery(Guid UsuarioId);
