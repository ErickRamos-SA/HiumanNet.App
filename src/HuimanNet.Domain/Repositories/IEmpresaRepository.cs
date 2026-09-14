using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de empresas cliente.
/// </summary>
public interface IEmpresaRepository
{
    /// <summary>
    /// Obtiene una empresa por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la empresa.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La empresa encontrada, o <c>null</c> si no existe.</returns>
    Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las empresas registradas.
    /// </summary>
    /// <param name="soloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas ordenadas por razón social.</returns>
    Task<IReadOnlyList<Empresa>> ListarAsync(
        bool soloActivas = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una nueva empresa.
    /// </summary>
    /// <param name="empresa">Empresa a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza los datos de una empresa existente.
    /// </summary>
    /// <param name="empresa">Empresa con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default);
}
