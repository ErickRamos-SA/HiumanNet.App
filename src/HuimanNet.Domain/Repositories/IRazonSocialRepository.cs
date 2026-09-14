using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de razones sociales.
/// </summary>
/// <remarks>
/// Todas las lecturas exigen la empresa cliente como filtro: el aislamiento
/// entre empresas se aplica en la consulta.
/// </remarks>
public interface IRazonSocialRepository
{
    /// <summary>
    /// Obtiene una razón social por su identificador, restringida a una empresa.
    /// </summary>
    /// <param name="id">Identificador de la razón social.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La razón social, o <c>null</c> si no existe o pertenece a otra empresa.</returns>
    Task<RazonSocial?> ObtenerPorIdAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las razones sociales de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="soloActivas">Si es <c>true</c>, omite las dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las razones sociales ordenadas por nombre.</returns>
    Task<IReadOnlyList<RazonSocial>> ListarPorEmpresaAsync(
        Guid empresaId, bool soloActivas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una razón social.
    /// </summary>
    /// <param name="razonSocial">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(RazonSocial razonSocial, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una razón social existente.
    /// </summary>
    /// <param name="razonSocial">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(RazonSocial razonSocial, CancellationToken cancellationToken = default);
}
