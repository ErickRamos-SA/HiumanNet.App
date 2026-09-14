using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Coteja una corrida contra el archivo de resultados del equipo de nómina.
/// </summary>
/// <param name="CorridaId">Corrida a cotejar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DocumentoId">Archivo de resultados o de ajuste del mismo período, ya disponible.</param>
/// <param name="ToleranciaAbsoluta">Diferencia absoluta aceptada por concepto.</param>
public sealed record CotejarNominaCommand(Guid CorridaId, Guid? EmpresaId, Guid DocumentoId, decimal ToleranciaAbsoluta);

/// <summary>
/// Lista los cotejos de una corrida.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarCotejosQuery(Guid CorridaId, Guid? EmpresaId);

/// <summary>
/// Obtiene un cotejo con su detalle.
/// </summary>
/// <param name="CotejoId">Cotejo consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerCotejoQuery(Guid CotejoId, Guid? EmpresaId);

/// <summary>
/// Ejecuta <see cref="CotejarNominaCommand"/>: lee el archivo manual, lo
/// empareja con los resultados del sistema por clave de trabajador y concepto,
/// y registra las diferencias.
/// </summary>
/// <remarks>
/// El archivo puede venir en formato <b>largo</b> (columnas Clave, Concepto e
/// Importe) o <b>ancho</b> (columna Clave y una columna por concepto, con la
/// clave del catálogo o con el encabezado de la hoja de nómina del modelo de
/// referencia). Si un trabajador tiene varios contratos que calculan el mismo
/// concepto, el sistema compara la suma.
/// <para>
/// El archivo manual no viaja en la petición: es un documento de resultados o
/// de ajuste publicado en el mismo período que la corrida.
/// </para>
/// </remarks>
public sealed class CotejarNominaHandler : IManejadorDeComando<CotejarNominaCommand, CotejoDto>
{
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly ICotejoRepository _cotejos;
    private readonly ArchivosDelPeriodo _archivos;
    private readonly ILectorDeArchivosTabulares _lector;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CotejarNominaHandler"/>.
    /// </summary>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="cotejos">Repositorio de cotejos.</param>
    /// <param name="archivos">Lectura de los archivos del período.</param>
    /// <param name="lector">Lector de archivos tabulares.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public CotejarNominaHandler(
        ICorridaDeNominaRepository corridas,
        ICotejoRepository cotejos,
        ArchivosDelPeriodo archivos,
        ILectorDeArchivosTabulares lector,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _corridas = corridas;
        _cotejos = cotejos;
        _archivos = archivos;
        _lector = lector;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<CotejoDto> EjecutarAsync(CotejarNominaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CotejarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(comando.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{comando.CorridaId}' no existe.");

        // Falla antes de leer el archivo si la corrida ya no admite cotejo.
        corrida.MarcarCotejada();

        ArchivoDelPeriodo archivo = await _archivos.LeerAsync(
            comando.DocumentoId, empresaId, [TipoDocumento.Resultado, TipoDocumento.Ajuste], cancellationToken);

        if (archivo.Documento.PeriodoId != corrida.PeriodoId)
        {
            throw new DocumentoInvalidoException("El archivo del cálculo manual debe pertenecer al mismo período que la corrida.");
        }

        string nombreArchivo = archivo.Documento.NombreOriginal.Valor;
        IReadOnlyList<ResultadoDeNomina> resultados = await _corridas.ListarResultadosAsync(corrida.Id, empresaId, cancellationToken);
        TablaLeida tabla = _lector.Leer(nombreArchivo, archivo.Contenido, hojasPreferidas: ["Nomina", "Resultados"]);

        IReadOnlyList<DiferenciaDeCotejo> diferencias =
            LectorDeResultadosManuales.Comparar(tabla, resultados, comando.ToleranciaAbsoluta);

        CotejoDeNomina cotejo = CotejoDeNomina.Registrar(
            corrida.Id, empresaId, _autorizador.Usuario.UsuarioId, nombreArchivo,
            comando.ToleranciaAbsoluta, diferencias, _reloj.GetUtcNow());

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _cotejos.AgregarAsync(cotejo, cancellationToken);
        await _corridas.ActualizarAsync(corrida, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CotejoDeNomina, empresaId, nameof(CorridaDeNomina), corrida.Id,
            $"documento={archivo.Documento.Id}; comparaciones={cotejo.TotalComparaciones}; fuera={cotejo.TotalFueraDeTolerancia}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(cotejo, _autorizador.Usuario.NombreCompleto, incluirDetalle: true);
    }
}

/// <summary>
/// Ejecuta <see cref="ListarCotejosQuery"/> y <see cref="ObtenerCotejoQuery"/>.
/// </summary>
public sealed class ConsultarCotejosHandler
    : IManejadorDeConsulta<ListarCotejosQuery, IReadOnlyList<CotejoDto>>,
      IManejadorDeConsulta<ObtenerCotejoQuery, CotejoDto>
{
    private readonly IConsultasNomina _consultas;
    private readonly ICotejoRepository _cotejos;
    private readonly IUsuarioRepository _usuarios;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarCotejosHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de nómina.</param>
    /// <param name="cotejos">Repositorio de cotejos.</param>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarCotejosHandler(
        IConsultasNomina consultas, ICotejoRepository cotejos, IUsuarioRepository usuarios, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _cotejos = cotejos;
        _usuarios = usuarios;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CotejoDto>> EjecutarAsync(
        ListarCotejosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        return await _consultas.ListarCotejosAsync(consulta.CorridaId, empresaId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CotejoDto> EjecutarAsync(ObtenerCotejoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        CotejoDeNomina cotejo = await _cotejos.ObtenerAsync(consulta.CotejoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El cotejo '{consulta.CotejoId}' no existe.");

        Usuario? usuario = await _usuarios.ObtenerPorIdAsync(cotejo.UsuarioId, cancellationToken);

        return Mapeadores.ADto(cotejo, usuario?.NombreCompleto ?? "(usuario desconocido)", incluirDetalle: true);
    }
}

/// <summary>
/// Interpreta el archivo de resultados manual y lo compara con los resultados del sistema.
/// </summary>
public static class LectorDeResultadosManuales
{
    /// <summary>
    /// Encabezados de la hoja de nómina del modelo de referencia y el concepto
    /// del catálogo con el que se comparan.
    /// </summary>
    private static readonly Dictionary<string, string> Alias = new(StringComparer.Ordinal)
    {
        ["DIASTRABAJADOS"] = "DIAS_TRABAJADOS_FISCAL",
        ["FALTAS"] = "FALTAS_FISCAL",
        ["SUELDO"] = "SUELDO",
        ["AGUINALDO"] = "AGUINALDO_FISCAL",
        ["VACACIONES"] = "VACACIONES_FISCAL",
        ["PRIMAVACACIONAL"] = "PRIMA_VACACIONAL",
        ["GRATIFICACION"] = "GRATIFICACION_FISCAL",
        ["TELETRABAJO"] = "TELETRABAJO_FISCAL",
        ["TOTALPERCEPCIONES"] = ClavesDeResumen.TotalPercepciones,
        ["SUBSIDIOEMPLEO"] = ClavesDeResumen.SubsidioEntregado,
        ["SUBSIDIO"] = ClavesDeResumen.SubsidioEntregado,
        ["ISR"] = ClavesDeResumen.Isr,
        ["IMSS"] = ClavesDeResumen.ImssTrabajador,
        ["CREDITOFONACOT"] = ClavesDeResumen.Fonacot,
        ["FONACOT"] = ClavesDeResumen.Fonacot,
        ["CREDITOINFONAVIT"] = ClavesDeResumen.InfonavitTrabajador,
        ["INFONAVIT"] = ClavesDeResumen.InfonavitTrabajador,
        ["PENSIONALIMENTICIA"] = "PENSION_ALIMENTICIA",
        ["TOTALDEDUCCIONES"] = ClavesDeResumen.TotalDeducciones,
        ["NETOPAGADO"] = ClavesDeResumen.NetoPagado,
        ["NETO"] = ClavesDeResumen.NetoPagado,
        ["SINDICATO"] = ClavesDeResumen.ComplementoSindical,
        ["COMPLEMENTOSINDICAL"] = ClavesDeResumen.ComplementoSindical,
        ["TOTALDEPERCEPCIONES"] = ClavesDeResumen.BrutoIncidencias,
        ["BRUTO"] = ClavesDeResumen.BrutoIncidencias,
        ["TOTALNOMINA"] = ClavesDeResumen.TotalNominaFacturable,
        ["ISN"] = ClavesDeResumen.Isn,
        ["COMISION"] = ClavesDeResumen.Comision,
        ["SUMA"] = ClavesDeResumen.CostoTotal,
        ["COSTOTOTAL"] = ClavesDeResumen.CostoTotal,
        ["BASEGRAVABLE"] = "BASE_GRAVABLE",
        ["IMPUESTODETERMINADO"] = "ISR_DETERMINADO",
        ["CUOTAOBRERA"] = ClavesDeResumen.ImssTrabajador,
    };

    /// <summary>
    /// Compara el archivo manual con los resultados del sistema.
    /// </summary>
    /// <param name="tabla">Archivo manual leído.</param>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="tolerancia">Diferencia absoluta aceptada.</param>
    /// <returns>Una comparación por trabajador y concepto presente en el archivo.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si el archivo no tiene columna de clave de trabajador.</exception>
    public static IReadOnlyList<DiferenciaDeCotejo> Comparar(
        TablaLeida tabla, IReadOnlyList<ResultadoDeNomina> resultados, decimal tolerancia)
    {
        ArgumentNullException.ThrowIfNull(tabla);
        ArgumentNullException.ThrowIfNull(resultados);

        int columnaClave = tabla.IndiceDe("Clave", "Clave empleado", "ClaveEmpleado", "Trabajador", "Empleado", "NOI", "Numero");

        if (columnaClave < 0)
        {
            throw new NominaInvalidaException(
                "El archivo debe tener una columna 'Clave' con la clave del trabajador.");
        }

        ILookup<string, ResultadoDeNomina> porClave = resultados.ToLookup(static r => r.ClaveEmpleado, StringComparer.OrdinalIgnoreCase);
        var conceptosDelSistema = new HashSet<string>(
            resultados.SelectMany(static r => r.Conceptos.Select(static c => c.Clave)), StringComparer.Ordinal);

        int columnaConcepto = tabla.IndiceDe("Concepto", "Clave concepto", "ConceptoClave");
        int columnaImporte = tabla.IndiceDe("Importe", "Valor", "Monto");
        var diferencias = new List<DiferenciaDeCotejo>();

        if (columnaConcepto >= 0 && columnaImporte >= 0)
        {
            foreach (string?[] fila in tabla.Filas)
            {
                string? clave = TablaLeida.Texto(fila, columnaClave);
                string? concepto = TablaLeida.Texto(fila, columnaConcepto);

                if (clave is null || concepto is null || !TablaLeida.Numero(fila, columnaImporte, out decimal importe))
                {
                    continue;
                }

                string claveConcepto = ResolverConcepto(concepto, conceptosDelSistema) ?? concepto.Trim().ToUpperInvariant();
                diferencias.Add(Comparar(clave, claveConcepto, importe, porClave[clave], tolerancia));
            }
        }
        else
        {
            var columnas = new List<(int Indice, string Concepto)>();

            for (int i = 0; i < tabla.Encabezados.Count; i++)
            {
                if (i == columnaClave)
                {
                    continue;
                }

                string? concepto = ResolverConcepto(tabla.Encabezados[i], conceptosDelSistema);

                if (concepto is not null)
                {
                    columnas.Add((i, concepto));
                }
            }

            foreach (string?[] fila in tabla.Filas)
            {
                string? clave = TablaLeida.Texto(fila, columnaClave);

                if (clave is null)
                {
                    continue;
                }

                foreach ((int indice, string concepto) in columnas)
                {
                    if (TablaLeida.Numero(fila, indice, out decimal importe))
                    {
                        diferencias.Add(Comparar(clave, concepto, importe, porClave[clave], tolerancia));
                    }
                }
            }
        }

        return diferencias
            .OrderBy(static d => d.ClaveEmpleado, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static d => d.ConceptoClave, StringComparer.Ordinal)
            .ToList();
    }

    private static string? ResolverConcepto(string encabezado, HashSet<string> conceptosDelSistema)
    {
        string normalizado = TablaLeida.Normalizar(encabezado);

        if (normalizado.Length == 0)
        {
            return null;
        }

        foreach (string clave in conceptosDelSistema)
        {
            if (TablaLeida.Normalizar(clave) == normalizado)
            {
                return clave;
            }
        }

        return Alias.TryGetValue(normalizado, out string? alias) ? alias : null;
    }

    private static DiferenciaDeCotejo Comparar(
        string clave, string concepto, decimal importeManual, IEnumerable<ResultadoDeNomina> resultados, decimal tolerancia)
    {
        decimal? importeSistema = null;
        Guid? contratoId = null;

        foreach (ResultadoDeNomina resultado in resultados)
        {
            foreach (ValorDeConcepto valor in resultado.Conceptos)
            {
                if (valor.Clave == concepto)
                {
                    importeSistema = (importeSistema ?? 0m) + valor.Importe;
                    contratoId ??= resultado.ContratoId;
                }
            }
        }

        decimal manual = Math.Round(importeManual, 2, MidpointRounding.AwayFromZero);
        decimal sistema = Math.Round(importeSistema ?? 0m, 2, MidpointRounding.AwayFromZero);
        decimal diferencia = sistema - manual;

        return new DiferenciaDeCotejo(
            clave.Trim(),
            contratoId,
            concepto,
            importeSistema is null ? null : sistema,
            manual,
            diferencia,
            importeSistema is not null && Math.Abs(diferencia) <= tolerancia);
    }
}
