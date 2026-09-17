using System.Globalization;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta las consultas de corridas y resultados.
/// </summary>
public sealed class ConsultarNominaHandler
    : IManejadorDeConsulta<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>>,
      IManejadorDeConsulta<ObtenerResumenDeCorridaQuery, ResumenDeCorridaDto>,
      IManejadorDeConsulta<ObtenerDetalleDeResultadoQuery, DetalleDeResultadoDto>,
      IManejadorDeConsulta<ExportarCorridaQuery, ArchivoExportado>
{
    private readonly IConsultasNomina _consultas;
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly IEmpleadoRepository _empleados;
    private readonly IIncidenciaRepository _incidencias;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarNominaHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de nómina.</param>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="empleados">Repositorio de empleados.</param>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="constructor">Resolución del catálogo.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarNominaHandler(
        IConsultasNomina consultas,
        ICorridaDeNominaRepository corridas,
        IRazonSocialRepository razonesSociales,
        IEmpleadoRepository empleados,
        IIncidenciaRepository incidencias,
        ConstructorDePlanDeCalculo constructor,
        AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _corridas = corridas;
        _razonesSociales = razonesSociales;
        _empleados = empleados;
        _incidencias = incidencias;
        _constructor = constructor;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CorridaDeNominaDto>> EjecutarAsync(
        ListarCorridasQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        IReadOnlyList<CorridaDeNominaDto> corridas = await _consultas.ListarCorridasAsync(consulta.PeriodoId, empresaId, cancellationToken);

        // La empresa cliente consulta el resultado vigente en solo lectura: las
        // corridas reemplazadas o descartadas son historial interno de nómina.
        return _autorizador.EsTransversal
            ? corridas
            : corridas.Where(static c => c.Estado != EstadoDeCorrida.Descartada).ToList();
    }

    /// <inheritdoc/>
    public async Task<ResumenDeCorridaDto> EjecutarAsync(
        ObtenerResumenDeCorridaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        CorridaDeNominaDto corrida = await _consultas.ObtenerCorridaAsync(consulta.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{consulta.CorridaId}' no existe.");

        IReadOnlyList<ResultadoDeNominaDto> resultados = await _consultas.ListarResultadosAsync(corrida.Id, empresaId, cancellationToken);
        IReadOnlyList<RazonSocial> razonesSociales = await _razonesSociales.ListarPorEmpresaAsync(empresaId, soloActivas: false, cancellationToken);

        // La tasa general sólo hace falta si alguna razón social no define la suya.
        decimal? tasaIvaGeneral = razonesSociales.Any(static r => r.Configuracion.TasaIva is null)
            ? (await _constructor.ResolverAsync(empresaId, corrida.FechaDeReferencia, cancellationToken))
                .ParametroObligatorio(ClavesDeParametro.TasaIva)
            : null;

        return new ResumenDeCorridaDto(
            corrida, resultados, CalculadoraDeFacturacion.Calcular(resultados, razonesSociales, tasaIvaGeneral ?? 0m));
    }

    /// <inheritdoc/>
    public async Task<DetalleDeResultadoDto> EjecutarAsync(
        ObtenerDetalleDeResultadoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        ResultadoDeNomina resultado = await _corridas.ObtenerResultadoAsync(consulta.ResultadoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El resultado '{consulta.ResultadoId}' no existe.");

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(resultado.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{resultado.CorridaId}' no existe.");

        ResultadoDeNominaDto resumen = await _consultas.ObtenerResultadoAsync(resultado.Id, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El resultado '{consulta.ResultadoId}' no existe.");

        // Definiciones vigentes en la fecha de la corrida, para mostrar la fórmula junto a cada importe.
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, corrida.FechaDeReferencia, cancellationToken);
        // Una misma clave existe una vez por esquema (TOTAL_PERCEPCIONES en IMSS,
        // sindicato y honorarios): sólo cuentan las del esquema del resultado.
        var definiciones = new Dictionary<string, ConceptoDeNomina>(StringComparer.Ordinal);

        foreach (ConceptoDeNomina definicion in catalogo.Conceptos.Where(c => c.Esquemas.Incluye(resultado.Esquema)))
        {
            definiciones.TryAdd(definicion.Clave, definicion);
        }

        var conceptos = new List<ConceptoCalculadoDto>(resultado.Conceptos.Count);

        foreach (ValorDeConcepto valor in resultado.Conceptos)
        {
            conceptos.Add(definiciones.TryGetValue(valor.Clave, out ConceptoDeNomina? definicion)
                ? new ConceptoCalculadoDto(valor.Clave, definicion.Nombre, definicion.Tipo, definicion.Formula, valor.Importe, definicion.VisibleEnRecibo)
                : new ConceptoCalculadoDto(valor.Clave, valor.Clave, TipoDeConcepto.Base, string.Empty, valor.Importe, false));
        }

        IReadOnlyList<ValorDto> variables = await ReconstruirVariablesAsync(resultado, corrida, catalogo, cancellationToken);

        return new DetalleDeResultadoDto(resumen, conceptos, variables);
    }

    /// <inheritdoc/>
    public async Task<ArchivoExportado> EjecutarAsync(
        ExportarCorridaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(consulta.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{consulta.CorridaId}' no existe.");

        IReadOnlyList<ResultadoDeNomina> resultados = await _corridas.ListarResultadosAsync(corrida.Id, empresaId, cancellationToken);
        IReadOnlyList<RazonSocial> razonesSociales = await _razonesSociales.ListarPorEmpresaAsync(empresaId, soloActivas: false, cancellationToken);

        byte[] contenido = ExportadorDeCorridas.ACsv(resultados, razonesSociales.ToDictionary(static r => r.Id, static r => r.Nombre));
        string nombre = string.Create(CultureInfo.InvariantCulture, $"nomina-corrida-{corrida.Numero}-{corrida.Id:N}.csv");

        return new ArchivoExportado(nombre, "text/csv", contenido);
    }

    /// <summary>
    /// Vuelve a calcular las variables de entrada de un resultado, para mostrar
    /// en el detalle con qué datos se calculó.
    /// </summary>
    /// <param name="resultado">Resultado del contrato.</param>
    /// <param name="corrida">Corrida a la que pertenece.</param>
    /// <param name="catalogo">Catálogo vigente en la fecha de la corrida.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Las variables; vacía si el contrato o su razón social ya no existen o el
    /// catálogo actual no permite reconstruirlas.
    /// </returns>
    private async Task<IReadOnlyList<ValorDto>> ReconstruirVariablesAsync(
        ResultadoDeNomina resultado, CorridaDeNomina corrida, CatalogoResuelto catalogo, CancellationToken cancellationToken)
    {
        Contrato? contrato = await _empleados.ObtenerContratoAsync(resultado.ContratoId, resultado.EmpresaId, cancellationToken);
        RazonSocial? razonSocial = contrato is null
            ? null
            : await _razonesSociales.ObtenerPorIdAsync(contrato.RazonSocialId, resultado.EmpresaId, cancellationToken);

        if (contrato is null || razonSocial is null)
        {
            return [];
        }

        Incidencia? incidencia = await _incidencias.ObtenerPorContratoAsync(
            corrida.PeriodoId, contrato.Id, resultado.EmpresaId, cancellationToken);

        try
        {
            IReadOnlyDictionary<string, decimal> variables =
                ConstructorDeVariables.Construir(contrato, razonSocial, incidencia, catalogo.Parametros, corrida.FechaDeReferencia);

            return variables.Select(static v => new ValorDto(v.Key, v.Value)).ToList();
        }
        catch (CatalogoInvalidoException)
        {
            return [];
        }
    }
}
