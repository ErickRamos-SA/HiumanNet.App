namespace HuimanNet.Contracts.Common;

/// <summary>
/// Cambio de contraseña por el propio usuario.
/// </summary>
/// <param name="ContrasenaActual">Contraseña vigente.</param>
/// <param name="NuevaContrasena">Nueva contraseña.</param>
public sealed record CambiarContrasenaRequest(string ContrasenaActual, string NuevaContrasena);
