namespace HuimanNet.Contracts.Empleados;

/// <summary>
/// Proyección de lectura completa de un empleado, con sus contratos.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="EmpresaId">Empresa cliente.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido.</param>
/// <param name="NombreCompleto">Nombre completo para mostrar.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="Curp">CURP.</param>
/// <param name="Nss">Número de seguridad social.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="Correo">Correo personal.</param>
/// <param name="Telefono">Teléfono.</param>
/// <param name="Activo">Estado.</param>
/// <param name="FechaAlta">Fecha de alta en el sistema.</param>
/// <param name="Contratos">Contratos del empleado.</param>
public sealed record EmpleadoDto(
    Guid Id,
    Guid EmpresaId,
    string Clave,
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string NombreCompleto,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono,
    bool Activo,
    DateTimeOffset FechaAlta,
    IReadOnlyList<ContratoDto> Contratos);
