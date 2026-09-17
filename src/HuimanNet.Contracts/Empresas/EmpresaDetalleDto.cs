namespace HuimanNet.Contracts.Empresas;

/// <summary>
/// Detalle administrativo de una empresa cliente.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">RFC o identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
/// <param name="FechaAlta">Fecha de alta, en UTC.</param>
/// <param name="TotalRazonesSociales">Razones sociales activas.</param>
/// <param name="TotalEmpleados">Empleados activos.</param>
/// <param name="TotalUsuarios">Usuarios activos.</param>
public sealed record EmpresaDetalleDto(
    Guid Id,
    string RazonSocial,
    string IdentificadorFiscal,
    bool Activa,
    DateTimeOffset FechaAlta,
    int TotalRazonesSociales,
    int TotalEmpleados,
    int TotalUsuarios);
