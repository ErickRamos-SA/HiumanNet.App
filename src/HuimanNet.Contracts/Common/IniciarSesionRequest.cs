namespace HuimanNet.Contracts.Common;

/// <summary>
/// Credenciales de inicio de sesión en modo local.
/// </summary>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Contrasena">Contraseña en claro; viaja sólo por HTTPS y nunca se almacena.</param>
public sealed record IniciarSesionRequest(string Correo, string Contrasena);
