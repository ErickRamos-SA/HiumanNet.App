using HuimanNet.Application.Common;
using HuimanNet.Web.Seguridad;

namespace HuimanNet.Web.Services;

/// <summary>
/// Ejecuta casos de uso desde los componentes, cada uno en su propio ámbito de DI.
/// </summary>
/// <remarks>
/// En Blazor Server un servicio <i>scoped</i> vive tanto como el circuito. La
/// sesión SQL es <i>scoped</i> y mantiene una conexión: sin este ejecutor, cada
/// usuario conectado retendría una conexión durante horas y dos componentes que
/// consultaran a la vez chocarían sobre ella. Aquí cada operación abre un ámbito,
/// usa una conexión del <i>pool</i> y la devuelve al terminar, que es el patrón
/// recomendado para acceso a datos en Blazor Server.
/// </remarks>
public sealed class EjecutorDeCasosDeUso
{
    private readonly IServiceScopeFactory _fabricaDeAmbitos;
    private readonly ContextoDeUsuarioDelCircuito _usuario;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EjecutorDeCasosDeUso"/>.
    /// </summary>
    /// <param name="fabricaDeAmbitos">Fábrica de ámbitos de DI.</param>
    /// <param name="usuario">Identidad del circuito.</param>
    public EjecutorDeCasosDeUso(IServiceScopeFactory fabricaDeAmbitos, ContextoDeUsuarioDelCircuito usuario)
    {
        _fabricaDeAmbitos = fabricaDeAmbitos;
        _usuario = usuario;
    }

    /// <summary>
    /// Ejecuta una consulta.
    /// </summary>
    /// <typeparam name="TConsulta">Tipo de la consulta.</typeparam>
    /// <typeparam name="TResultado">Tipo del resultado.</typeparam>
    /// <param name="consulta">Consulta a ejecutar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado de la consulta.</returns>
    public Task<TResultado> ConsultarAsync<TConsulta, TResultado>(TConsulta consulta, CancellationToken cancellationToken = default)
        => EnAmbitoAsync(sp => sp.GetRequiredService<IManejadorDeConsulta<TConsulta, TResultado>>().EjecutarAsync(consulta, cancellationToken));

    /// <summary>
    /// Ejecuta un comando que devuelve un resultado.
    /// </summary>
    /// <typeparam name="TComando">Tipo del comando.</typeparam>
    /// <typeparam name="TResultado">Tipo del resultado.</typeparam>
    /// <param name="comando">Comando a ejecutar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado del comando.</returns>
    public Task<TResultado> EjecutarAsync<TComando, TResultado>(TComando comando, CancellationToken cancellationToken = default)
        => EnAmbitoAsync(sp => sp.GetRequiredService<IManejadorDeComando<TComando, TResultado>>().EjecutarAsync(comando, cancellationToken));

    /// <summary>
    /// Ejecuta un comando sin resultado.
    /// </summary>
    /// <typeparam name="TComando">Tipo del comando.</typeparam>
    /// <param name="comando">Comando a ejecutar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public Task EjecutarAsync<TComando>(TComando comando, CancellationToken cancellationToken = default)
        => EnAmbitoAsync(async sp =>
        {
            await sp.GetRequiredService<IManejadorDeComando<TComando>>().EjecutarAsync(comando, cancellationToken);
            return true;
        });

    /// <summary>
    /// Ejecuta una operación en un ámbito de DI nuevo que ya conoce la identidad del circuito.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado.</typeparam>
    /// <param name="operacion">Operación que resuelve y ejecuta el caso de uso.</param>
    /// <returns>El resultado de la operación.</returns>
    /// <remarks>
    /// Sólo se exponen casos de uso: las páginas nunca resuelven repositorios ni
    /// consultas directamente, así la autorización y el ámbito de empresa los
    /// aplica siempre la capa de aplicación.
    /// </remarks>
    private async Task<T> EnAmbitoAsync<T>(Func<IServiceProvider, Task<T>> operacion)
    {
        await using AsyncServiceScope ambito = _fabricaDeAmbitos.CreateAsyncScope();

        // Antes de resolver nada que dependa de IUsuarioActual.
        ambito.ServiceProvider.GetRequiredService<PortadorDeUsuarioActual>().Usuario = _usuario;

        return await operacion(ambito.ServiceProvider);
    }
}
