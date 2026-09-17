namespace HuimanNet.Contracts.Empleados;

/// <summary>
/// Petición para crear o actualizar un empleado.
/// </summary>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="Curp">CURP.</param>
/// <param name="Nss">Número de seguridad social.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="Correo">Correo personal.</param>
/// <param name="Telefono">Teléfono.</param>
/// <param name="Activo">Estado.</param>
public sealed record GuardarEmpleadoRequest(
    Guid? EmpresaId,
    string Clave,
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono,
    bool Activo = true);
