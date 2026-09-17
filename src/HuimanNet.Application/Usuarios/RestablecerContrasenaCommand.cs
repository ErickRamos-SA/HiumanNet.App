namespace HuimanNet.Application.Usuarios;

/// <summary>Restablece la contraseña local de un usuario (administrador).</summary>
/// <param name="UsuarioId">Usuario afectado.</param>
/// <param name="NuevaContrasena">Contraseña temporal.</param>
public sealed record RestablecerContrasenaCommand(Guid UsuarioId, string NuevaContrasena);
