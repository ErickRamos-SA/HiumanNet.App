using HuimanNet.Contracts.Empleados;

namespace HuimanNet.Application.Empleados;

/// <summary>
/// Crea o actualiza un empleado.
/// </summary>
/// <param name="EmpleadoId">Empleado a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del empleado.</param>
public sealed record GuardarEmpleadoCommand(Guid? EmpleadoId, GuardarEmpleadoRequest Datos);
