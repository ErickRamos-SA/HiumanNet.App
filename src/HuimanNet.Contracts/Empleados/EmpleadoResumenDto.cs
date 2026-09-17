namespace HuimanNet.Contracts.Empleados;

/// <summary>
/// Proyección resumida de un empleado para listados.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="NombreCompleto">Nombre completo.</param>
/// <param name="Activo">Estado.</param>
/// <param name="ContratosActivos">Número de contratos vigentes.</param>
/// <param name="Esquemas">Esquemas de sus contratos vigentes, separados por coma.</param>
/// <param name="RazonesSociales">Razones sociales de sus contratos vigentes, separadas por coma.</param>
/// <param name="EmpresaId">Empresa a la que pertenece el empleado.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa, para los listados de varias empresas.</param>
public sealed record EmpleadoResumenDto(
    Guid Id,
    string Clave,
    string NombreCompleto,
    bool Activo,
    int ContratosActivos,
    string Esquemas,
    string RazonesSociales,
    Guid EmpresaId,
    string EmpresaRazonSocial);
