using HuimanNet.Domain.Entities;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de períodos de carga.
/// </summary>
/// <remarks>
/// Todas las lecturas exigen la empresa del solicitante como filtro obligatorio:
/// el aislamiento entre empresas se aplica en la consulta, no en la capa superior.
/// </remarks>
public interface IPeriodoRepository
{
    /// <summary>
    /// Obtiene un período por su identificador, restringido a una empresa.
    /// </summary>
    /// <param name="id">Identificador del período.</param>
    /// <param name="empresaId">Empresa del usuario solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El período encontrado, o <c>null</c> si no existe o pertenece a otra empresa.</returns>
    Task<PeriodoCarga?> ObtenerPorIdAsync(
        Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el período de una empresa en una posición concreta del calendario.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="calendario">Año, mes y consecutivo buscados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El período encontrado, o <c>null</c> si no se ha abierto.</returns>
    Task<PeriodoCarga?> ObtenerPorCalendarioAsync(
        Guid empresaId, PeriodoCalendario calendario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los períodos de una empresa, del más reciente al más antiguo.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="incluirCerrados">Si es <c>false</c>, omite los períodos ya cerrados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los períodos de la empresa.</returns>
    Task<IReadOnlyList<PeriodoCarga>> ListarPorEmpresaAsync(
        Guid empresaId, bool incluirCerrados = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los períodos con documentos pendientes de procesar por el operador de nómina.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los períodos de todas las empresas en estado <c>Recibido</c> o <c>EnProceso</c>.</returns>
    /// <remarks>Consulta transversal: sólo debe invocarse con rol de operador o administrador.</remarks>
    Task<IReadOnlyList<PeriodoCarga>> ListarBandejaDelOperadorAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un nuevo período.
    /// </summary>
    /// <param name="periodo">Período a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(PeriodoCarga periodo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un período existente.
    /// </summary>
    /// <param name="periodo">Período con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(PeriodoCarga periodo, CancellationToken cancellationToken = default);
}
