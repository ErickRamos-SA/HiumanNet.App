namespace HuimanNet.Application.Usuarios;

/// <summary>Cambia la contraseña del propio usuario.</summary>
/// <param name="ContrasenaActual">Contraseña vigente.</param>
/// <param name="NuevaContrasena">Nueva contraseña.</param>
public sealed record CambiarContrasenaCommand(string ContrasenaActual, string NuevaContrasena);
