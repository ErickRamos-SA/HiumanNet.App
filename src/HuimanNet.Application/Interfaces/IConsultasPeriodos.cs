using HuimanNet.Contracts.Periodos;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de los períodos de carga, con el resumen de documentos que
/// necesitan las bandejas de cliente y de operador.
/// </summary>
public interface IConsultasPeriodos
{
    /// <summary>
    /// Obtiene un período concreto de una empresa.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El período, o <c>null</c> si no existe o pertenece a otra empresa.</returns>
    Task<PeriodoDto?> ObtenerAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los períodos de una empresa, del más reciente al más antiguo.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="incluirCerrados">Si es <c>false</c>, omite los períodos cerrados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los períodos de la empresa.</returns>
    Task<IReadOnlyList<PeriodoDto>> ListarPorEmpresaAsync(
        Guid empresaId, bool incluirCerrados, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista la bandeja transversal del operador de nómina.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Los períodos de todas las empresas que esperan acción del operador,
    /// ordenados por antigüedad de la primera recepción.
    /// </returns>
    /// <remarks>
    /// Consulta deliberadamente sin filtro de empresa. El manejador que la
    /// invoca debe haber verificado antes que el rol es operador o administrador.
    /// </remarks>
    Task<IReadOnlyList<PeriodoDto>> ListarBandejaDelOperadorAsync(
        CancellationToken cancellationToken = default);
}
