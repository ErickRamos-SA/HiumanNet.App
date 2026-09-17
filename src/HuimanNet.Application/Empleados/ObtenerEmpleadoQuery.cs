namespace HuimanNet.Application.Empleados;

/// <summary>
/// Obtiene un empleado con sus contratos.
/// </summary>
/// <param name="EmpleadoId">Empleado consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerEmpleadoQuery(Guid EmpleadoId, Guid? EmpresaId);
