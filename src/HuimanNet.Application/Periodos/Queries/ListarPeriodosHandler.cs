using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Periodos.Queries;

/// <summary>
/// Ejecuta <see cref="ListarPeriodosQuery"/>.
/// </summary>
public sealed class ListarPeriodosHandler
    : IManejadorDeConsulta<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>>
{
    private readonly IConsultasPeriodos _consultas;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarPeriodosHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de períodos.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    public ListarPeriodosHandler(
        IConsultasPeriodos consultas,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="Domain.Exceptions.AccesoNoAutorizadoException">
    /// Se lanza si el solicitante pide períodos de una empresa que no le corresponde.
    /// </exception>
    public async Task<IReadOnlyList<PeriodoDto>> EjecutarAsync(
        ListarPeriodosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        Guid empresaId = _politicaDeAcceso.ResolverEmpresaObjetivo(
            _usuarioActual.Rol, _usuarioActual.EmpresaId, _usuarioActual.Empresas, consulta.EmpresaId);

        return await _consultas.ListarPorEmpresaAsync(
            empresaId, consulta.IncluirCerrados, cancellationToken);
    }
}
