namespace HuimanNet.Application.Usuarios;

/// <summary>Inicia sesión con credenciales locales.</summary>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Contrasena">Contraseña en claro.</param>
/// <param name="DireccionIp">Dirección IP de origen, para la bitácora.</param>
public sealed record IniciarSesionLocalCommand(string Correo, string Contrasena, string? DireccionIp);
