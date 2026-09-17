namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Valida credenciales locales para abrir una sesión en el portal web, sin
/// emitir un token de acceso.
/// </summary>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Contrasena">Contraseña en claro.</param>
/// <param name="DireccionIp">Dirección IP de origen, para la bitácora.</param>
public sealed record ValidarCredencialesLocalesCommand(string Correo, string Contrasena, string? DireccionIp);
