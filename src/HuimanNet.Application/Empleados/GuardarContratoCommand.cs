using HuimanNet.Contracts.Empleados;

namespace HuimanNet.Application.Empleados;

/// <summary>
/// Crea o actualiza un contrato de un empleado.
/// </summary>
/// <param name="EmpleadoId">Empleado propietario.</param>
/// <param name="ContratoId">Contrato a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del contrato.</param>
public sealed record GuardarContratoCommand(Guid EmpleadoId, Guid? ContratoId, GuardarContratoRequest Datos);
