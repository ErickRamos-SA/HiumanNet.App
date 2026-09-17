using HuimanNet.Contracts.Incidencias;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de incidencias.
/// </summary>
public interface IConsultasIncidencias
{
    /// <summary>
    /// Lista una fila por cada contrato vigente en el período, con la incidencia
    /// capturada o, si no la hay, con las cantidades de un período completo.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="fechaDeReferencia">Fecha con la que se determina la vigencia de los contratos.</param>
    /// <param name="diasPeriodoPredeterminados">Días del período para las filas sin captura.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las filas ordenadas por clave de empleado.</returns>
    Task<IReadOnlyList<IncidenciaDto>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, DateOnly fechaDeReferencia, decimal diasPeriodoPredeterminados,
        CancellationToken cancellationToken = default);
}
