using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de corridas de nómina y de sus resultados.
/// </summary>
/// <remarks>
/// Los resultados de una corrida se insertan en lote: para una empresa con
/// miles de trabajadores, insertar fila a fila multiplicaría los viajes a la
/// base de datos.
/// </remarks>
public interface ICorridaDeNominaRepository
{
    /// <summary>
    /// Obtiene una corrida por su identificador.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La corrida, o <c>null</c>.</returns>
    Task<CorridaDeNomina?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las corridas de un período, de la más reciente a la más antigua.
    /// </summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas del período.</returns>
    Task<IReadOnlyList<CorridaDeNomina>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el siguiente número consecutivo de corrida de un período.
    /// </summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El número que debe llevar la próxima corrida.</returns>
    Task<int> SiguienteNumeroAsync(Guid periodoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una corrida con todos sus resultados en lote.
    /// </summary>
    /// <param name="corrida">Corrida a persistir.</param>
    /// <param name="resultados">Resultados por contrato.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(
        CorridaDeNomina corrida, IReadOnlyList<ResultadoDeNomina> resultados, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el estado y las notas de una corrida.
    /// </summary>
    /// <param name="corrida">Corrida con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(CorridaDeNomina corrida, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los resultados de una corrida, con su detalle de conceptos.
    /// </summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los resultados ordenados por clave de empleado.</returns>
    Task<IReadOnlyList<ResultadoDeNomina>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un resultado con su detalle de conceptos.
    /// </summary>
    /// <param name="resultadoId">Identificador del resultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado, o <c>null</c>.</returns>
    Task<ResultadoDeNomina?> ObtenerResultadoAsync(
        Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default);
}
