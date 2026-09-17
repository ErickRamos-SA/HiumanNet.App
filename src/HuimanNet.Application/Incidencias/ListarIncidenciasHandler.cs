using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Ejecuta <see cref="ListarIncidenciasQuery"/>.
/// </summary>
public sealed class ListarIncidenciasHandler : IManejadorDeConsulta<ListarIncidenciasQuery, IReadOnlyList<IncidenciaDto>>
{
    private readonly IConsultasIncidencias _consultas;
    private readonly IPeriodoRepository _periodos;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarIncidenciasHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de incidencias.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="constructor">Resolución del catálogo, para los días predeterminados.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ListarIncidenciasHandler(
        IConsultasIncidencias consultas,
        IPeriodoRepository periodos,
        ConstructorDePlanDeCalculo constructor,
        AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _periodos = periodos;
        _constructor = constructor;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IncidenciaDto>> EjecutarAsync(
        ListarIncidenciasQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(consulta.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{consulta.PeriodoId}' no existe para la empresa.");

        DateOnly fecha = periodo.FechaDeReferencia;
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        decimal dias = catalogo.ParametroObligatorio(ClavesDeParametro.DiasPeriodoPredeterminados);

        return await _consultas.ListarPorPeriodoAsync(periodo.Id, empresaId, fecha, dias, cancellationToken);
    }
}
