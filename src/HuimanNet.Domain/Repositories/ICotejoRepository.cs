using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de cotejos.
/// </summary>
public interface ICotejoRepository
{
    /// <summary>
    /// Inserta un cotejo con su detalle.
    /// </summary>
    /// <param name="cotejo">Cotejo a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(CotejoDeNomina cotejo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un cotejo con su detalle.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El cotejo, o <c>null</c>.</returns>
    Task<CotejoDeNomina?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los cotejos de una corrida, sin detalle, del más reciente al más antiguo.
    /// </summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los cotejos de la corrida.</returns>
    Task<IReadOnlyList<CotejoDeNomina>> ListarPorCorridaAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);
}
