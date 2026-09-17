namespace HuimanNet.Domain.Entities;

/// <summary>
/// Datos personales de un empleado.
/// </summary>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido, o <c>null</c>.</param>
/// <param name="Rfc">RFC, o <c>null</c> si aún no se captura.</param>
/// <param name="Curp">CURP, o <c>null</c>.</param>
/// <param name="Nss">Número de seguridad social, o <c>null</c>.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento, o <c>null</c>.</param>
/// <param name="Correo">Correo personal, o <c>null</c>.</param>
/// <param name="Telefono">Teléfono, o <c>null</c>.</param>
/// <remarks>RFC, CURP y NSS son datos sensibles: nunca deben escribirse en registros de log.</remarks>
public sealed record DatosPersonales(
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono);
