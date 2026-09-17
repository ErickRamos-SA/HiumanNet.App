using HuimanNet.Contracts.Empresas;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura del catálogo de empresas cliente.
/// </summary>
public interface IConsultasEmpresas
{
    /// <summary>
    /// Lista las empresas registradas.
    /// </summary>
    /// <param name="soloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas ordenadas por razón social.</returns>
    /// <remarks>
    /// Sólo la consumen los roles transversales: un usuario de empresa cliente
    /// no tiene por qué conocer el catálogo completo.
    /// </remarks>
    Task<IReadOnlyList<EmpresaDto>> ListarAsync(
        bool soloActivas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una empresa por su identificador.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La empresa, o <c>null</c> si no existe.</returns>
    Task<EmpresaDto?> ObtenerAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las empresas con sus contadores administrativos.
    /// </summary>
    /// <param name="soloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas ordenadas por razón social.</returns>
    Task<IReadOnlyList<EmpresaDetalleDto>> ListarDetalleAsync(
        bool soloActivas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle administrativo de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El detalle, o <c>null</c> si no existe.</returns>
    Task<EmpresaDetalleDto?> ObtenerDetalleAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
