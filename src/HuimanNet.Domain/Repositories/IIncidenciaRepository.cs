using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de incidencias de período.
/// </summary>
public interface IIncidenciaRepository
{
    /// <summary>
    /// Obtiene una incidencia por su identificador.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La incidencia, o <c>null</c>.</returns>
    Task<Incidencia?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la incidencia de un contrato en un período.
    /// </summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La incidencia, o <c>null</c> si no se ha capturado.</returns>
    Task<Incidencia?> ObtenerPorContratoAsync(
        Guid periodoId, Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las incidencias de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las incidencias capturadas del período.</returns>
    Task<IReadOnlyList<Incidencia>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una incidencia.
    /// </summary>
    /// <param name="incidencia">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(Incidencia incidencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una incidencia existente.
    /// </summary>
    /// <param name="incidencia">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(Incidencia incidencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una incidencia.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);
}
