namespace HuimanNet.Application.Common;

/// <summary>
/// Manejador de una consulta de sólo lectura.
/// </summary>
/// <typeparam name="TConsulta">Tipo de la consulta de entrada.</typeparam>
/// <typeparam name="TResultado">Tipo del resultado devuelto.</typeparam>
public interface IManejadorDeConsulta<in TConsulta, TResultado>
{
    /// <summary>
    /// Ejecuta la consulta.
    /// </summary>
    /// <param name="consulta">Filtros de la consulta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado de la consulta.</returns>
    Task<TResultado> EjecutarAsync(TConsulta consulta, CancellationToken cancellationToken = default);
}
