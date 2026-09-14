using System.Globalization;
using System.Text;
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
/// Lista las corridas de un período.
/// </summary>
/// <param name="PeriodoId">Período consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarCorridasQuery(Guid PeriodoId, Guid? EmpresaId);

/// <summary>
/// Obtiene una corrida con sus resultados y la facturación estimada.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerResumenDeCorridaQuery(Guid CorridaId, Guid? EmpresaId);

/// <summary>
/// Obtiene el detalle de conceptos de un resultado.
/// </summary>
/// <param name="ResultadoId">Resultado consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerDetalleDeResultadoQuery(Guid ResultadoId, Guid? EmpresaId);

/// <summary>
/// Exporta una corrida a CSV.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ExportarCorridaQuery(Guid CorridaId, Guid? EmpresaId);

/// <summary>
/// Archivo generado para descarga.
/// </summary>
/// <param name="Nombre">Nombre sugerido.</param>
/// <param name="TipoDeContenido">Tipo MIME.</param>
/// <param name="Contenido">Bytes del archivo.</param>
public sealed record ArchivoExportado(string Nombre, string TipoDeContenido, byte[] Contenido);

/// <summary>
/// Cambia el estado de una corrida (aprobar o descartar).
/// </summary>
/// <param name="CorridaId">Corrida afectada.</param>
/// <param name="Estado">Estado destino.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CambiarEstadoCorridaCommand(Guid CorridaId, EstadoDeCorrida Estado, Guid? EmpresaId, string? Observaciones);

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

        return new ResumenDeCorridaDto(corrida, resultados, CalculadoraDeFacturacion.Calcular(resultados, razonesSociales));
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

/// <summary>
/// Ejecuta <see cref="CambiarEstadoCorridaCommand"/>.
/// </summary>
public sealed class CambiarEstadoCorridaHandler : IManejadorDeComando<CambiarEstadoCorridaCommand>
{
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CambiarEstadoCorridaHandler"/>.
    /// </summary>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    public CambiarEstadoCorridaHandler(
        ICorridaDeNominaRepository corridas,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork)
    {
        _corridas = corridas;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(CambiarEstadoCorridaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.AprobarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(comando.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{comando.CorridaId}' no existe.");

        EstadoDeCorrida anterior = corrida.Estado;

        switch (comando.Estado)
        {
            case EstadoDeCorrida.Aprobada:
                corrida.Aprobar(comando.Observaciones);
                break;
            case EstadoDeCorrida.Descartada:
                corrida.Descartar(comando.Observaciones);
                break;
            default:
                throw new NominaInvalidaException("Sólo puede aprobarse o descartarse una corrida.");
        }

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _corridas.ActualizarAsync(corrida, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CambioEstadoCorrida, empresaId, nameof(CorridaDeNomina), corrida.Id,
            $"{anterior}->{comando.Estado}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }
}

/// <summary>
/// Agrupa los resultados de una corrida por razón social y estima la factura
/// con la misma estructura que la hoja de facturación del modelo de referencia.
/// </summary>
public static class CalculadoraDeFacturacion
{
    /// <summary>
    /// Calcula la facturación por razón social.
    /// </summary>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="razonesSociales">Razones sociales de la empresa, para el tipo de servicio y la tasa de IVA.</param>
    /// <returns>Una fila por razón social con resultados.</returns>
    public static IReadOnlyList<FacturacionDeCorridaDto> Calcular(
        IReadOnlyList<ResultadoDeNominaDto> resultados, IReadOnlyList<RazonSocial> razonesSociales)
    {
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(razonesSociales);

        var configuracion = razonesSociales.ToDictionary(static r => r.Id);
        var filas = new List<FacturacionDeCorridaDto>();

        foreach (IGrouping<Guid, ResultadoDeNominaDto> grupo in resultados.GroupBy(static r => r.RazonSocialId))
        {
            configuracion.TryGetValue(grupo.Key, out RazonSocial? razonSocial);

            decimal baseNomina = 0, baseFiniquitos = 0, isn = 0, isr = 0, imss = 0, infonavit = 0, otros = 0, comision = 0;
            int trabajadores = 0;

            foreach (ResultadoDeNominaDto r in grupo)
            {
                trabajadores++;

                if (r.TipoDeMovimiento == TipoDeMovimiento.Finiquito)
                {
                    baseFiniquitos += r.Facturable;
                }
                else
                {
                    baseNomina += r.Facturable;
                }

                isn += r.Isn;
                isr += r.CostoIsr;
                imss += r.CostoImss;
                infonavit += r.CostoInfonavit;
                otros += r.CostoOtros;
                comision += r.Comision;
            }

            decimal subtotal = baseNomina + baseFiniquitos + isn + isr + imss + infonavit + otros + comision;
            decimal tasaIva = razonSocial?.Configuracion.TasaIva ?? 0m;
            decimal iva = Math.Round(subtotal * tasaIva, 2, MidpointRounding.AwayFromZero);

            filas.Add(new FacturacionDeCorridaDto(
                grupo.Key,
                razonSocial?.Nombre ?? grupo.First().RazonSocialNombre,
                razonSocial?.Configuracion.TipoDeServicio ?? TipoDeServicio.NoEspecificado,
                trabajadores, baseNomina, baseFiniquitos, isn, isr, imss, infonavit, otros, comision,
                subtotal, iva, subtotal + iva));
        }

        return filas.OrderBy(static f => f.RazonSocialNombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}

/// <summary>
/// Genera el archivo CSV de una corrida: una fila por contrato con todos los
/// conceptos como columnas.
/// </summary>
/// <remarks>
/// El archivo usa el mismo formato que acepta el cotejo (formato ancho), de
/// modo que el equipo de nómina puede tomarlo como plantilla para su resultado
/// manual.
/// </remarks>
public static class ExportadorDeCorridas
{
    /// <summary>
    /// Serializa los resultados a CSV con codificación UTF-8 y marca de orden de bytes.
    /// </summary>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="nombresDeRazonSocial">Nombres de razón social por identificador.</param>
    /// <returns>Bytes del archivo.</returns>
    public static byte[] ACsv(IReadOnlyList<ResultadoDeNomina> resultados, IReadOnlyDictionary<Guid, string> nombresDeRazonSocial)
    {
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(nombresDeRazonSocial);

        List<string> columnas = resultados
            .SelectMany(static r => r.Conceptos.Select(static c => c.Clave))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("Clave,Nombre,RazonSocial,Esquema,Movimiento");

        foreach (string columna in columnas)
        {
            sb.Append(',').Append(columna);
        }

        sb.AppendLine();

        foreach (ResultadoDeNomina r in resultados)
        {
            var valores = r.Conceptos.ToDictionary(static c => c.Clave, static c => c.Importe, StringComparer.Ordinal);

            sb.Append(Escapar(r.ClaveEmpleado)).Append(',')
              .Append(Escapar(r.NombreEmpleado)).Append(',')
              .Append(Escapar(nombresDeRazonSocial.TryGetValue(r.RazonSocialId, out string? nombre) ? nombre : string.Empty)).Append(',')
              .Append(r.Esquema).Append(',')
              .Append(r.TipoDeMovimiento);

            foreach (string columna in columnas)
            {
                sb.Append(',');

                if (valores.TryGetValue(columna, out decimal importe))
                {
                    sb.Append(Math.Round(importe, 4, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture));
                }
            }

            sb.AppendLine();
        }

        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] cuerpo = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] resultado = new byte[bom.Length + cuerpo.Length];
        bom.CopyTo(resultado, 0);
        cuerpo.CopyTo(resultado, bom.Length);
        return resultado;
    }

    private static string Escapar(string valor)
        => valor.Contains(',', StringComparison.Ordinal) || valor.Contains('"', StringComparison.Ordinal)
            ? "\"" + valor.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : valor;
}
