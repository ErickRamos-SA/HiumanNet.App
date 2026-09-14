using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Periodos.Queries;

/// <summary>
/// Consulta la bandeja transversal del operador de nómina.
/// </summary>
public sealed record ListarBandejaOperadorQuery;

/// <summary>
/// Ejecuta <see cref="ListarBandejaOperadorQuery"/>: devuelve, de todas las
/// empresas, los períodos que esperan acción del operador.
/// </summary>
/// <remarks>
/// Es la única consulta del sistema que atraviesa deliberadamente la frontera
/// entre empresas, y por eso comprueba el rol antes de tocar la base de datos.
/// </remarks>
public sealed class ListarBandejaOperadorHandler
    : IManejadorDeConsulta<ListarBandejaOperadorQuery, IReadOnlyList<PeriodoDto>>
{
    private readonly IConsultasPeriodos _consultas;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarBandejaOperadorHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de períodos.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    public ListarBandejaOperadorHandler(
        IConsultasPeriodos consultas,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
    }

    /// <inheritdoc/>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol del solicitante no es transversal.
    /// </exception>
    public async Task<IReadOnlyList<PeriodoDto>> EjecutarAsync(
        ListarBandejaOperadorQuery consulta, CancellationToken cancellationToken = default)
    {
        if (!_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{_usuarioActual.Rol}' no puede consultar la bandeja del operador.");
        }

        return await _consultas.ListarBandejaDelOperadorAsync(cancellationToken);
    }
}
