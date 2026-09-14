namespace HuimanNet.Application.Common;

/// <summary>
/// Manejador de un comando que devuelve un resultado.
/// </summary>
/// <typeparam name="TComando">Tipo del comando de entrada.</typeparam>
/// <typeparam name="TResultado">Tipo del resultado devuelto.</typeparam>
/// <remarks>
/// La solución implementa <b>CQRS ligero sin mediador</b>: cada manejador se
/// registra e inyecta de forma explícita. Se descartó MediatR porque su
/// despacho por reflexión es incompatible con la publicación Native AOT de la
/// API (ESPECIFICACION.md §6).
/// </remarks>
public interface IManejadorDeComando<in TComando, TResultado>
{
    /// <summary>
    /// Ejecuta el caso de uso.
    /// </summary>
    /// <param name="comando">Datos de entrada del caso de uso.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado del caso de uso.</returns>
    Task<TResultado> EjecutarAsync(TComando comando, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manejador de un comando que no devuelve resultado.
/// </summary>
/// <typeparam name="TComando">Tipo del comando de entrada.</typeparam>
public interface IManejadorDeComando<in TComando>
{
    /// <summary>
    /// Ejecuta el caso de uso.
    /// </summary>
    /// <param name="comando">Datos de entrada del caso de uso.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EjecutarAsync(TComando comando, CancellationToken cancellationToken = default);
}

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
