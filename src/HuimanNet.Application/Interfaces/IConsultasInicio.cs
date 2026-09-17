namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de los datos del panel de inicio.
/// </summary>
/// <remarks>
/// Sólo lee: qué se sugiere a cada usuario lo decide
/// <see cref="Inicio.ObtenerResumenDeInicioHandler"/>.
/// </remarks>
public interface IConsultasInicio
{
    /// <summary>
    /// Lee los indicadores, los períodos no cerrados más recientes y las
    /// corridas calculadas pendientes de cotejo.
    /// </summary>
    /// <param name="empresaId">Empresa consultada, o <c>null</c> para todas.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los datos del panel.</returns>
    Task<Inicio.DatosDeInicio> ObtenerDatosAsync(Guid? empresaId, CancellationToken cancellationToken = default);
}
