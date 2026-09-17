using HuimanNet.Contracts.Nomina;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de corridas, resultados y cotejos.
/// </summary>
public interface IConsultasNomina
{
    /// <summary>
    /// Lista las corridas de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas de la más reciente a la más antigua.</returns>
    Task<IReadOnlyList<CorridaDeNominaDto>> ListarCorridasAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las corridas más recientes de una empresa o de todas.
    /// </summary>
    /// <param name="empresaId">Empresa, o <c>null</c> para todas.</param>
    /// <param name="limite">Número máximo de corridas.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas de la más reciente a la más antigua.</returns>
    Task<IReadOnlyList<CorridaDeNominaDto>> ListarRecientesAsync(
        Guid? empresaId, int limite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La corrida, o <c>null</c>.</returns>
    Task<CorridaDeNominaDto?> ObtenerCorridaAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los resultados de una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los resultados ordenados por razón social y clave de empleado.</returns>
    Task<IReadOnlyList<ResultadoDeNominaDto>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el resumen de un resultado.
    /// </summary>
    /// <param name="resultadoId">Resultado consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado, o <c>null</c>.</returns>
    Task<ResultadoDeNominaDto?> ObtenerResultadoAsync(
        Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los cotejos de una corrida, sin detalle.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los cotejos del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<CotejoDto>> ListarCotejosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);
}
