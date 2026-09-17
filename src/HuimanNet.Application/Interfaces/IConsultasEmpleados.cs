using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de empleados y contratos.
/// </summary>
public interface IConsultasEmpleados
{
    /// <summary>
    /// Lista los empleados de forma paginada.
    /// </summary>
    /// <param name="empresas">Empresas consultadas, o <c>null</c> para todas (sólo roles transversales).</param>
    /// <param name="soloActivos">Si es <c>true</c>, omite los dados de baja.</param>
    /// <param name="texto">Texto a buscar en clave o nombre, o <c>null</c>.</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamanoPagina">Elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La página de empleados, ordenada por empresa y clave.</returns>
    Task<PaginaDto<EmpleadoResumenDto>> ListarAsync(
        IReadOnlyCollection<Guid>? empresas, bool soloActivos, string? texto, int pagina, int tamanoPagina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un empleado con sus contratos.
    /// </summary>
    /// <param name="empleadoId">Empleado consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El empleado, o <c>null</c>.</returns>
    Task<EmpleadoDto?> ObtenerAsync(Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default);
}
