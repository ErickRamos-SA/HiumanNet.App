using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Captura o actualiza la incidencia de un contrato en un período.
/// </summary>
/// <param name="Datos">Datos de la incidencia.</param>
public sealed record GuardarIncidenciaCommand(GuardarIncidenciaRequest Datos);

/// <summary>
/// Elimina la incidencia capturada de un contrato en un período.
/// </summary>
/// <param name="IncidenciaId">Incidencia a eliminar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record EliminarIncidenciaCommand(Guid IncidenciaId, Guid? EmpresaId);

/// <summary>
/// Importa las incidencias de un período desde uno de sus archivos de incidencias.
/// </summary>
/// <param name="DocumentoId">Documento de tipo incidencias del período, ya disponible.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <remarks>
/// El archivo no viaja en la petición: se toma del período, adonde llegó por el
/// flujo de carga con análisis antimalware. Así todo intercambio de archivos
/// queda asociado a un período y registrado en la bitácora.
/// </remarks>
public sealed record ImportarIncidenciasCommand(Guid DocumentoId, Guid? EmpresaId);

/// <summary>
/// Lista las incidencias de un período, una fila por contrato vigente.
/// </summary>
/// <param name="PeriodoId">Período consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarIncidenciasQuery(Guid PeriodoId, Guid? EmpresaId);

/// <summary>
/// Ejecuta la captura, actualización y eliminación de incidencias.
/// </summary>
/// <remarks>
/// Las incidencias pueden capturarse mientras el período no esté cerrado:
/// el cálculo del sistema se repite en corridas nuevas tantas veces como haga
/// falta durante la etapa de cotejo.
/// </remarks>
public sealed class GuardarIncidenciaHandler
    : IManejadorDeComando<GuardarIncidenciaCommand, IncidenciaDto>,
      IManejadorDeComando<EliminarIncidenciaCommand>
{
    private readonly IIncidenciaRepository _incidencias;
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GuardarIncidenciaHandler"/>.
    /// </summary>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public GuardarIncidenciaHandler(
        IIncidenciaRepository incidencias,
        IPeriodoRepository periodos,
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _incidencias = incidencias;
        _periodos = periodos;
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<IncidenciaDto> EjecutarAsync(GuardarIncidenciaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);

        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        GuardarIncidenciaRequest d = comando.Datos;
        Guid empresaId = _autorizador.ResolverEmpresa(d.EmpresaId);

        PeriodoCarga periodo = await ObtenerPeriodoAbiertoAsync(d.PeriodoId, empresaId, cancellationToken);

        Contrato contrato = await _empleados.ObtenerContratoAsync(d.ContratoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El contrato '{d.ContratoId}' no existe.");

        Empleado empleado = await _empleados.ObtenerPorIdAsync(contrato.EmpleadoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException("El empleado del contrato no existe.");

        RazonSocial? razonSocial = await _razonesSociales.ObtenerPorIdAsync(contrato.RazonSocialId, empresaId, cancellationToken);

        var datos = new DatosDeIncidencia(
            d.DiasPeriodo, d.Vacaciones, d.Ausentismos, d.Incapacidades, d.Festivos, d.HorasDobles, d.HorasTriples,
            d.DomingosTrabajados, d.Gratificacion, d.Reembolsos, d.Teletrabajo, d.Finiquito, d.Cafeteria,
            d.HorasDescontadas, d.OtrosDescuentos, d.PrestamoPersonal, d.Aguinaldo, d.DescuentosFiscales,
            d.FonacotCapturado, d.DescuentoSindicalAdicional, d.AjusteSindical, d.IsrManual, d.TipoDeMovimiento,
            d.Observaciones);

        Incidencia? existente = await _incidencias.ObtenerPorContratoAsync(periodo.Id, contrato.Id, empresaId, cancellationToken);
        DateTimeOffset ahora = _reloj.GetUtcNow();
        Incidencia incidencia;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (existente is null)
        {
            incidencia = Incidencia.Registrar(empresaId, periodo.Id, contrato.Id, datos, _autorizador.Usuario.UsuarioId, ahora);
            await _incidencias.AgregarAsync(incidencia, cancellationToken);
        }
        else
        {
            incidencia = existente;
            incidencia.Actualizar(datos, _autorizador.Usuario.UsuarioId, ahora);
            await _incidencias.ActualizarAsync(incidencia, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(Incidencia), incidencia.Id,
            $"periodo={periodo.Calendario.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return ADto(incidencia, contrato, empleado, razonSocial?.Nombre ?? string.Empty, _autorizador.Usuario.NombreCompleto);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarIncidenciaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        Incidencia incidencia = await _incidencias.ObtenerAsync(comando.IncidenciaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La incidencia '{comando.IncidenciaId}' no existe.");

        await ObtenerPeriodoAbiertoAsync(incidencia.PeriodoId, empresaId, cancellationToken);

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _incidencias.EliminarAsync(incidencia.Id, empresaId, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(Incidencia), incidencia.Id, "eliminacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    private async Task<PeriodoCarga> ObtenerPeriodoAbiertoAsync(Guid periodoId, Guid empresaId, CancellationToken cancellationToken)
    {
        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(periodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{periodoId}' no existe para la empresa.");

        return periodo.Estado == EstadoPeriodo.Cerrado
            ? throw new PeriodoCerradoException(periodo.Id, periodo.Estado)
            : periodo;
    }

    /// <summary>
    /// Proyecta una incidencia a su DTO.
    /// </summary>
    /// <param name="incidencia">Incidencia capturada.</param>
    /// <param name="contrato">Contrato al que pertenece.</param>
    /// <param name="empleado">Empleado del contrato.</param>
    /// <param name="razonSocialNombre">Nombre de la razón social.</param>
    /// <param name="capturadoPor">Nombre de quien capturó.</param>
    /// <returns>El DTO equivalente.</returns>
    public static IncidenciaDto ADto(Incidencia incidencia, Contrato contrato, Empleado empleado, string razonSocialNombre, string capturadoPor)
    {
        ArgumentNullException.ThrowIfNull(incidencia);
        ArgumentNullException.ThrowIfNull(contrato);
        ArgumentNullException.ThrowIfNull(empleado);
        DatosDeIncidencia d = incidencia.Datos;

        return new IncidenciaDto(
            incidencia.Id, incidencia.PeriodoId, contrato.Id, empleado.Id, empleado.Clave, empleado.NombreCompleto,
            razonSocialNombre, contrato.Esquema, d.DiasPeriodo, d.Vacaciones, d.Ausentismos, d.Incapacidades, d.Festivos,
            d.HorasDobles, d.HorasTriples, d.DomingosTrabajados, d.Gratificacion, d.Reembolsos, d.Teletrabajo, d.Finiquito,
            d.Cafeteria, d.HorasDescontadas, d.OtrosDescuentos, d.PrestamoPersonal, d.Aguinaldo, d.DescuentosFiscales,
            d.FonacotCapturado, d.DescuentoSindicalAdicional, d.AjusteSindical, d.IsrManual, d.TipoDeMovimiento,
            d.Observaciones, capturadoPor, incidencia.FechaCaptura);
    }
}

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

        DateOnly fecha = FechasDePeriodo.Referencia(periodo);
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        decimal dias = catalogo.Parametros.TryGetValue(ClavesDeParametro.DiasPeriodoPredeterminados, out decimal valor) ? valor : 7m;

        return await _consultas.ListarPorPeriodoAsync(periodo.Id, empresaId, fecha, dias, cancellationToken);
    }
}

/// <summary>
/// Ejecuta <see cref="ImportarIncidenciasCommand"/>: lee un archivo de
/// incidencias del período, con el diseño de la hoja de incidencias del modelo
/// de referencia, y crea o actualiza la incidencia de cada trabajador.
/// </summary>
/// <remarks>
/// Los trabajadores se emparejan por la columna <c>Clave</c>. Si un trabajador
/// tiene varios contratos vigentes, la incidencia se aplica a todos salvo que
/// el archivo incluya una columna <c>Esquema</c> o <c>Razon Social</c> que
/// permita distinguirlos. Las filas con error se reportan sin detener la
/// importación del resto.
/// </remarks>
public sealed class ImportarIncidenciasHandler : IManejadorDeComando<ImportarIncidenciasCommand, ResultadoDeImportacionDto>
{
    private readonly IIncidenciaRepository _incidencias;
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly ArchivosDelPeriodo _archivos;
    private readonly ILectorDeArchivosTabulares _lector;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ImportarIncidenciasHandler"/>.
    /// </summary>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="archivos">Lectura de los archivos del período.</param>
    /// <param name="lector">Lector de archivos tabulares.</param>
    /// <param name="constructor">Resolución del catálogo.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public ImportarIncidenciasHandler(
        IIncidenciaRepository incidencias,
        IPeriodoRepository periodos,
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        ArchivosDelPeriodo archivos,
        ILectorDeArchivosTabulares lector,
        ConstructorDePlanDeCalculo constructor,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _incidencias = incidencias;
        _periodos = periodos;
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _archivos = archivos;
        _lector = lector;
        _constructor = constructor;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<ResultadoDeImportacionDto> EjecutarAsync(
        ImportarIncidenciasCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        ArchivoDelPeriodo archivo = await _archivos.LeerAsync(
            comando.DocumentoId, empresaId, [TipoDocumento.Incidencia], cancellationToken);

        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(archivo.Documento.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{archivo.Documento.PeriodoId}' no existe para la empresa.");

        if (periodo.Estado == EstadoPeriodo.Cerrado)
        {
            throw new PeriodoCerradoException(periodo.Id, periodo.Estado);
        }

        DateOnly fecha = FechasDePeriodo.Referencia(periodo);
        TablaLeida tabla = _lector.Leer(archivo.Documento.NombreOriginal.Valor, archivo.Contenido, hojasPreferidas: ["Incidencias"]);

        IReadOnlyList<Contrato> contratos = await _empleados.ListarContratosVigentesAsync(empresaId, fecha, cancellationToken);
        IReadOnlyList<Empleado> empleados = await _empleados.ListarPorEmpresaAsync(empresaId, soloActivos: true, cancellationToken);
        IReadOnlyList<RazonSocial> razonesSociales = await _razonesSociales.ListarPorEmpresaAsync(empresaId, soloActivas: false, cancellationToken);
        IReadOnlyList<Incidencia> existentes = await _incidencias.ListarPorPeriodoAsync(periodo.Id, empresaId, cancellationToken);
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        decimal diasPredeterminados = catalogo.Parametros.TryGetValue(ClavesDeParametro.DiasPeriodoPredeterminados, out decimal v) ? v : 7m;

        var contexto = new ContextoDeImportacion(
            empleados.ToDictionary(static e => e.Clave, StringComparer.OrdinalIgnoreCase),
            contratos.ToLookup(static c => c.EmpleadoId),
            razonesSociales.ToDictionary(static r => r.Id),
            existentes.ToDictionary(static i => i.ContratoId));

        ImportadorDeIncidencias.Plan plan = ImportadorDeIncidencias.Interpretar(tabla, contexto, diasPredeterminados);

        int creadas = 0, actualizadas = 0;
        DateTimeOffset ahora = _reloj.GetUtcNow();

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        foreach ((Guid contratoId, DatosDeIncidencia datos) in plan.Incidencias)
        {
            if (contexto.Existentes.TryGetValue(contratoId, out Incidencia? existente))
            {
                existente.Actualizar(datos, _autorizador.Usuario.UsuarioId, ahora);
                await _incidencias.ActualizarAsync(existente, cancellationToken);
                actualizadas++;
            }
            else
            {
                Incidencia nueva = Incidencia.Registrar(empresaId, periodo.Id, contratoId, datos, _autorizador.Usuario.UsuarioId, ahora);
                await _incidencias.AgregarAsync(nueva, cancellationToken);
                creadas++;
            }
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(PeriodoCarga), periodo.Id,
            $"importacion; documento={archivo.Documento.Id}; filas={plan.FilasLeidas}; creadas={creadas}; actualizadas={actualizadas}; errores={plan.Errores.Count}",
            cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return new ResultadoDeImportacionDto(plan.FilasLeidas, creadas, actualizadas, plan.Errores);
    }
}

/// <summary>
/// Datos de referencia con los que se interpreta un archivo de incidencias.
/// </summary>
/// <param name="EmpleadosPorClave">Empleados activos por clave.</param>
/// <param name="ContratosPorEmpleado">Contratos vigentes por empleado.</param>
/// <param name="RazonesSociales">Razones sociales por identificador.</param>
/// <param name="Existentes">Incidencias ya capturadas por contrato.</param>
public sealed record ContextoDeImportacion(
    IReadOnlyDictionary<string, Empleado> EmpleadosPorClave,
    ILookup<Guid, Contrato> ContratosPorEmpleado,
    IReadOnlyDictionary<Guid, RazonSocial> RazonesSociales,
    IReadOnlyDictionary<Guid, Incidencia> Existentes);

/// <summary>
/// Interpreta la hoja de incidencias del modelo de referencia.
/// </summary>
/// <remarks>
/// Las columnas se reconocen por su encabezado, sin distinguir mayúsculas,
/// acentos ni espacios. Se aceptan los encabezados de la hoja
/// <i>Incidencias</i> del archivo de cálculo y las claves de variable del
/// motor, de modo que un archivo exportado por el propio sistema también es
/// importable.
/// </remarks>
public static class ImportadorDeIncidencias
{
    /// <summary>
    /// Resultado de interpretar el archivo.
    /// </summary>
    /// <param name="FilasLeidas">Filas con clave de trabajador.</param>
    /// <param name="Incidencias">Incidencias a guardar, por contrato.</param>
    /// <param name="Errores">Filas rechazadas, con el motivo.</param>
    public sealed record Plan(int FilasLeidas, IReadOnlyList<(Guid ContratoId, DatosDeIncidencia Datos)> Incidencias, IReadOnlyList<string> Errores);

    /// <summary>
    /// Interpreta la tabla y produce las incidencias a guardar.
    /// </summary>
    /// <param name="tabla">Archivo leído.</param>
    /// <param name="contexto">Datos de referencia de la empresa.</param>
    /// <param name="diasPredeterminados">Días del período cuando el archivo no los trae.</param>
    /// <returns>El plan de importación.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si el archivo no tiene columna de clave.</exception>
    public static Plan Interpretar(TablaLeida tabla, ContextoDeImportacion contexto, decimal diasPredeterminados)
    {
        ArgumentNullException.ThrowIfNull(tabla);
        ArgumentNullException.ThrowIfNull(contexto);

        int cClave = tabla.IndiceDe("Clave", "Clave empleado", "Trabajador", "NOI", "Numero", "No");

        if (cClave < 0)
        {
            throw new NominaInvalidaException("El archivo debe tener una columna 'Clave' con la clave del trabajador.");
        }

        int cDias = tabla.IndiceDe("Dias de Periodo", "Dias Periodo", "Dias del periodo", VariablesDeCalculo.DiasPeriodo);
        int cVac = tabla.IndiceDe("Vacaciones", VariablesDeCalculo.Vacaciones);
        int cAus = tabla.IndiceDe("Ausentismos", "Faltas", "Ausencias", VariablesDeCalculo.Ausentismos);
        int cInc = tabla.IndiceDe("Incapacidades", "Incapacidad", VariablesDeCalculo.Incapacidades);
        int cFes = tabla.IndiceDe("Cantidad festivo", "Festivos", "Cantidad festivos", VariablesDeCalculo.Festivos);
        int cDob = tabla.IndiceDe("Cantidad dobles", "Horas dobles", VariablesDeCalculo.HorasDobles);
        int cTri = tabla.IndiceDe("Cantidad triples", "Horas triples", VariablesDeCalculo.HorasTriples);
        int cDom = tabla.IndiceDe("Cantidad p dominical", "Domingos", "Prima dominical cantidad", VariablesDeCalculo.Domingos);
        int cGra = tabla.IndiceDe("Gratificacion / Bonos", "Gratificacion", "Bonos", VariablesDeCalculo.Gratificacion);
        int cRee = tabla.IndiceDe("Reembolsos y otros", "Reembolsos", VariablesDeCalculo.Reembolsos);
        int cTel = tabla.IndiceDe("Teletrabajo", VariablesDeCalculo.Teletrabajo);
        int cFin = tabla.IndiceDe("Finiquito", VariablesDeCalculo.Finiquito);
        int cCaf = tabla.IndiceDe("Gastos Cafeteria", "Cafeteria", VariablesDeCalculo.Cafeteria);
        int cHde = tabla.IndiceDe("Cantidad Horas", "Horas descontadas", VariablesDeCalculo.HorasDescontadas);
        int cOtr = tabla.IndiceDe("Otros Desc.", "Otros descuentos", "Otros Desc", VariablesDeCalculo.OtrosDescuentos);
        int cPre = tabla.IndiceDe("Descuento Prestamo Personal", "Prestamo personal", "Prestamo", VariablesDeCalculo.PrestamoPersonal);
        int cAgu = tabla.IndiceDe("Aguinaldo", VariablesDeCalculo.Aguinaldo);
        int cDfi = tabla.IndiceDe("Descuentos fiscales", "Otros", VariablesDeCalculo.DescuentosFiscales);
        int cFon = tabla.IndiceDe("Fonacot", "Credito Fonacot", VariablesDeCalculo.FonacotCapturado);
        int cDsa = tabla.IndiceDe("Descuento sindical adicional", "Descuentos", VariablesDeCalculo.DescuentoSindicalAdicional);
        int cAjs = tabla.IndiceDe("Ajuste sindical", VariablesDeCalculo.AjusteSindical);
        int cIsr = tabla.IndiceDe("ISR manual", "Ret Real", VariablesDeCalculo.IsrManual);
        int cMov = tabla.IndiceDe("Tipo de movimiento", "Movimiento", "Tipo movimiento");
        int cObs = tabla.IndiceDe("Observaciones", "Observacion", "Notas");
        int cEsq = tabla.IndiceDe("Esquema");
        int cRaz = tabla.IndiceDe("Razon Social", "RazonSocial");

        var incidencias = new List<(Guid, DatosDeIncidencia)>();
        var errores = new List<string>();
        int filasLeidas = 0;

        for (int i = 0; i < tabla.Filas.Count; i++)
        {
            string?[] fila = tabla.Filas[i];
            int numeroDeFila = tabla.PrimeraFilaDeDatos + i;
            string? clave = TablaLeida.Texto(fila, cClave);

            if (clave is null)
            {
                continue;
            }

            filasLeidas++;

            if (!contexto.EmpleadosPorClave.TryGetValue(clave, out Empleado? empleado))
            {
                errores.Add($"Fila {numeroDeFila}: no existe un empleado activo con clave '{clave}'.");
                continue;
            }

            List<Contrato> contratos = FiltrarContratos(contexto, empleado, TablaLeida.Texto(fila, cEsq), TablaLeida.Texto(fila, cRaz));

            if (contratos.Count == 0)
            {
                errores.Add($"Fila {numeroDeFila}: el empleado '{clave}' no tiene contratos vigentes que coincidan.");
                continue;
            }

            try
            {
                DatosDeIncidencia datos = Leer(
                    fila, diasPredeterminados, cDias, cVac, cAus, cInc, cFes, cDob, cTri, cDom, cGra, cRee, cTel, cFin,
                    cCaf, cHde, cOtr, cPre, cAgu, cDfi, cFon, cDsa, cAjs, cIsr, cMov, cObs);

                foreach (Contrato contrato in contratos)
                {
                    incidencias.Add((contrato.Id, datos));
                }
            }
            catch (FormatException)
            {
                errores.Add($"Fila {numeroDeFila}: alguna celda numérica contiene texto no válido.");
            }
            catch (NominaInvalidaException excepcion)
            {
                errores.Add($"Fila {numeroDeFila}: {excepcion.Message}");
            }
        }

        return new Plan(filasLeidas, incidencias, errores);
    }

    private static List<Contrato> FiltrarContratos(ContextoDeImportacion contexto, Empleado empleado, string? esquema, string? razonSocial)
    {
        IEnumerable<Contrato> contratos = contexto.ContratosPorEmpleado[empleado.Id];

        if (esquema is not null)
        {
            string e = TablaLeida.Normalizar(esquema);
            contratos = contratos.Where(c => TablaLeida.Normalizar(c.Esquema.ToString()) == e);
        }

        if (razonSocial is not null)
        {
            string r = TablaLeida.Normalizar(razonSocial);
            contratos = contratos.Where(c =>
                contexto.RazonesSociales.TryGetValue(c.RazonSocialId, out RazonSocial? rs)
                && TablaLeida.Normalizar(rs.Nombre) == r);
        }

        return contratos.ToList();
    }

    private static DatosDeIncidencia Leer(
        string?[] fila, decimal diasPredeterminados,
        int cDias, int cVac, int cAus, int cInc, int cFes, int cDob, int cTri, int cDom, int cGra, int cRee, int cTel,
        int cFin, int cCaf, int cHde, int cOtr, int cPre, int cAgu, int cDfi, int cFon, int cDsa, int cAjs, int cIsr,
        int cMov, int cObs)
    {
        decimal N(int indice) => TablaLeida.Numero(fila, indice, out decimal valor) ? valor : 0m;

        decimal dias = TablaLeida.Numero(fila, cDias, out decimal d) ? d : diasPredeterminados;
        decimal? isrManual = TablaLeida.Numero(fila, cIsr, out decimal isr) ? isr : null;
        string? movimiento = TablaLeida.Texto(fila, cMov);

        TipoDeMovimiento tipo = movimiento is not null && TablaLeida.Normalizar(movimiento).Contains("FINIQUITO", StringComparison.Ordinal)
            ? TipoDeMovimiento.Finiquito
            : TipoDeMovimiento.Ordinaria;

        return new DatosDeIncidencia(
            dias, N(cVac), N(cAus), N(cInc), N(cFes), N(cDob), N(cTri), N(cDom), N(cGra), N(cRee), N(cTel), N(cFin),
            N(cCaf), N(cHde), N(cOtr), N(cPre), N(cAgu), N(cDfi), N(cFon), N(cDsa), N(cAjs), isrManual, tipo,
            TablaLeida.Texto(fila, cObs));
    }
}
