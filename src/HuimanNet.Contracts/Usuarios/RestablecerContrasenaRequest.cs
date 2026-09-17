namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Petición del administrador para restablecer la contraseña de un usuario.
/// </summary>
/// <param name="NuevaContrasena">Contraseña temporal; el usuario deberá cambiarla.</param>
public sealed record RestablecerContrasenaRequest(string NuevaContrasena);
