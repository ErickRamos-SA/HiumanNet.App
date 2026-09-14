using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de empleados y de sus contratos.
/// </summary>
/// <remarks>
/// Todas las lecturas exigen la empresa cliente como filtro. Los contratos se
/// tratan como parte del agregado del empleado, pero se exponen métodos
/// específicos para que el cálculo pueda cargar en una sola consulta todos los
/// contratos vigentes de una empresa sin materializar cada empleado.
/// </remarks>
public interface IEmpleadoRepository
{
    /// <summary>
    /// Obtiene un empleado por su identificador.
    /// </summary>
    /// <param name="id">Identificador del empleado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El empleado, o <c>null</c> si no existe o pertenece a otra empresa.</returns>
    Task<Empleado?> ObtenerPorIdAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un empleado por su clave dentro de la empresa.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="clave">Clave del empleado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El empleado, o <c>null</c> si no existe.</returns>
    Task<Empleado?> ObtenerPorClaveAsync(Guid empresaId, string clave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los empleados de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="soloActivos">Si es <c>true</c>, omite los dados de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los empleados ordenados por clave.</returns>
    Task<IReadOnlyList<Empleado>> ListarPorEmpresaAsync(
        Guid empresaId, bool soloActivos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un empleado.
    /// </summary>
    /// <param name="empleado">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(Empleado empleado, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un empleado existente.
    /// </summary>
    /// <param name="empleado">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(Empleado empleado, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un contrato por su identificador.
    /// </summary>
    /// <param name="contratoId">Identificador del contrato.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El contrato, o <c>null</c> si no existe o pertenece a otra empresa.</returns>
    Task<Contrato?> ObtenerContratoAsync(Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los contratos de un empleado.
    /// </summary>
    /// <param name="empleadoId">Empleado consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los contratos, del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<Contrato>> ListarContratosDeEmpleadoAsync(
        Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los contratos de una empresa vigentes en una fecha.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="fecha">Fecha de referencia del período.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los contratos vigentes de empleados activos, ordenados por clave de empleado.</returns>
    /// <remarks>Es la consulta de entrada del cálculo: debe resolverse en una sola sentencia.</remarks>
    Task<IReadOnlyList<Contrato>> ListarContratosVigentesAsync(
        Guid empresaId, DateOnly fecha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un contrato.
    /// </summary>
    /// <param name="contrato">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un contrato existente.
    /// </summary>
    /// <param name="contrato">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default);
}
