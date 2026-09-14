using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Catalogos;

/// <summary>Crea o actualiza un parámetro.</summary>
/// <param name="ParametroId">Parámetro a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del parámetro.</param>
public sealed record GuardarParametroCommand(Guid? ParametroId, GuardarParametroRequest Datos);

/// <summary>Elimina un parámetro.</summary>
/// <param name="ParametroId">Parámetro a eliminar.</param>
public sealed record EliminarParametroCommand(Guid ParametroId);

/// <summary>Lista los parámetros globales y los de una empresa.</summary>
/// <param name="EmpresaId">Empresa, o <c>null</c> para sólo globales.</param>
public sealed record ListarParametrosQuery(Guid? EmpresaId);

/// <summary>Crea o actualiza una tabla por rangos.</summary>
/// <param name="TablaId">Tabla a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos de la tabla.</param>
public sealed record GuardarTablaCommand(Guid? TablaId, GuardarTablaRequest Datos);

/// <summary>Elimina una tabla.</summary>
/// <param name="TablaId">Tabla a eliminar.</param>
public sealed record EliminarTablaCommand(Guid TablaId);

/// <summary>Lista las tablas globales y las de una empresa.</summary>
/// <param name="EmpresaId">Empresa, o <c>null</c> para sólo globales.</param>
public sealed record ListarTablasQuery(Guid? EmpresaId);

/// <summary>Crea o actualiza un concepto.</summary>
/// <param name="ConceptoId">Concepto a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del concepto.</param>
public sealed record GuardarConceptoCommand(Guid? ConceptoId, GuardarConceptoRequest Datos);

/// <summary>Elimina un concepto.</summary>
/// <param name="ConceptoId">Concepto a eliminar.</param>
public sealed record EliminarConceptoCommand(Guid ConceptoId);

/// <summary>Lista los conceptos globales y los de una empresa.</summary>
/// <param name="EmpresaId">Empresa, o <c>null</c> para sólo globales.</param>
public sealed record ListarConceptosQuery(Guid? EmpresaId);

/// <summary>Prueba una fórmula con valores de ejemplo.</summary>
/// <param name="Datos">Fórmula y valores.</param>
public sealed record ProbarFormulaQuery(ProbarFormulaRequest Datos);

/// <summary>Crea o actualiza una sección de explicación.</summary>
/// <param name="ExplicacionId">Sección a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos de la sección.</param>
public sealed record GuardarExplicacionCommand(Guid? ExplicacionId, GuardarExplicacionRequest Datos);

/// <summary>Elimina una sección de explicación.</summary>
/// <param name="ExplicacionId">Sección a eliminar.</param>
public sealed record EliminarExplicacionCommand(Guid ExplicacionId);

/// <summary>Lista las secciones de explicación.</summary>
/// <param name="Esquema">Esquema a filtrar, o <c>null</c>.</param>
/// <param name="Idioma">Idioma a filtrar, o <c>null</c>.</param>
public sealed record ListarExplicacionesQuery(EsquemaDePago? Esquema, Idioma? Idioma);

/// <summary>Obtiene la explicación completa de un esquema con el catálogo vigente.</summary>
/// <param name="Esquema">Esquema explicado.</param>
/// <param name="Idioma">Idioma del texto.</param>
/// <param name="EmpresaId">Empresa cuyo catálogo se usa, o <c>null</c> para el global.</param>
/// <param name="Fecha">Fecha de referencia, o <c>null</c> para hoy.</param>
public sealed record ObtenerExplicacionCompletaQuery(EsquemaDePago Esquema, Idioma Idioma, Guid? EmpresaId, DateOnly? Fecha);

/// <summary>
/// Ejecuta los comandos de mantenimiento de parámetros, tablas, conceptos y explicaciones.
/// </summary>
/// <remarks>
/// Reservado a quien tenga la acción <see cref="AccionDelSistema.AdministrarCatalogosDeCalculo"/>.
/// Toda modificación queda en la bitácora: el catálogo <b>es</b> el algoritmo de la nómina.
/// </remarks>
public sealed class AdministrarCatalogosHandler
    : IManejadorDeComando<GuardarParametroCommand, ParametroDeCalculoDto>,
      IManejadorDeComando<EliminarParametroCommand>,
      IManejadorDeComando<GuardarTablaCommand, TablaDeRangosDto>,
      IManejadorDeComando<EliminarTablaCommand>,
      IManejadorDeComando<GuardarConceptoCommand, ConceptoDeNominaDto>,
      IManejadorDeComando<EliminarConceptoCommand>,
      IManejadorDeComando<GuardarExplicacionCommand, ExplicacionDeCalculoDto>,
      IManejadorDeComando<EliminarExplicacionCommand>
{
    private readonly ICatalogoDeCalculoRepository _catalogos;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AdministrarCatalogosHandler"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public AdministrarCatalogosHandler(
        ICatalogoDeCalculoRepository catalogos,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _catalogos = catalogos;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<ParametroDeCalculoDto> EjecutarAsync(GuardarParametroCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarParametroRequest d = comando.Datos;
        ParametroDeCalculo parametro;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ParametroId is null)
        {
            parametro = ParametroDeCalculo.Crear(
                d.Clave, d.Descripcion, d.Grupo, d.Valor, d.Unidad, d.EmpresaId, d.VigenteDesde, d.VigenteHasta, _reloj.GetUtcNow());
            await _catalogos.AgregarParametroAsync(parametro, cancellationToken);
        }
        else
        {
            parametro = await _catalogos.ObtenerParametroAsync(comando.ParametroId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El parámetro '{comando.ParametroId}' no existe.");
            parametro.Actualizar(d.Descripcion, d.Grupo, d.Valor, d.Unidad, d.VigenteDesde, d.VigenteHasta, _reloj.GetUtcNow());
            await _catalogos.ActualizarParametroAsync(parametro, cancellationToken);
        }

        await AuditarAsync("Parametro", parametro.Id, parametro.EmpresaId, $"clave={parametro.Clave}; valor={parametro.Valor}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(parametro);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarParametroCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ParametroDeCalculo parametro = await _catalogos.ObtenerParametroAsync(comando.ParametroId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El parámetro '{comando.ParametroId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarParametroAsync(parametro.Id, cancellationToken);
        await AuditarAsync("Parametro", parametro.Id, parametro.EmpresaId, $"eliminacion; clave={parametro.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TablaDeRangosDto> EjecutarAsync(GuardarTablaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarTablaRequest d = comando.Datos;
        IEnumerable<RangoDeTabla> rangos = (d.Rangos ?? [])
            .Select(static r => new RangoDeTabla(r.LimiteInferior, r.LimiteSuperior, r.CuotaFija, r.Porcentaje, r.Valor));

        TablaDeRangos tabla;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.TablaId is null)
        {
            tabla = TablaDeRangos.Crear(d.Clave, d.Descripcion, d.EmpresaId, d.VigenteDesde, d.VigenteHasta, rangos, _reloj.GetUtcNow());
            await _catalogos.AgregarTablaAsync(tabla, cancellationToken);
        }
        else
        {
            tabla = await _catalogos.ObtenerTablaAsync(comando.TablaId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"La tabla '{comando.TablaId}' no existe.");
            tabla.Actualizar(d.Descripcion, d.VigenteDesde, d.VigenteHasta, rangos, _reloj.GetUtcNow());
            await _catalogos.ActualizarTablaAsync(tabla, cancellationToken);
        }

        await AuditarAsync("Tabla", tabla.Id, tabla.EmpresaId, $"clave={tabla.Clave}; rangos={tabla.Rangos.Count}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(tabla);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarTablaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        TablaDeRangos tabla = await _catalogos.ObtenerTablaAsync(comando.TablaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La tabla '{comando.TablaId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarTablaAsync(tabla.Id, cancellationToken);
        await AuditarAsync("Tabla", tabla.Id, tabla.EmpresaId, $"eliminacion; clave={tabla.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ConceptoDeNominaDto> EjecutarAsync(GuardarConceptoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarConceptoRequest d = comando.Datos;
        ConceptoDeNomina concepto;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ConceptoId is null)
        {
            concepto = ConceptoDeNomina.Crear(
                d.Clave, d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, d.EmpresaId, _reloj.GetUtcNow());

            if (!d.Activo)
            {
                concepto.Actualizar(d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, false, _reloj.GetUtcNow());
            }

            await _catalogos.AgregarConceptoAsync(concepto, cancellationToken);
        }
        else
        {
            concepto = await _catalogos.ObtenerConceptoAsync(comando.ConceptoId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El concepto '{comando.ConceptoId}' no existe.");
            concepto.Actualizar(d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, d.Activo, _reloj.GetUtcNow());
            await _catalogos.ActualizarConceptoAsync(concepto, cancellationToken);
        }

        await AuditarAsync("Concepto", concepto.Id, concepto.EmpresaId, $"clave={concepto.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(concepto);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarConceptoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ConceptoDeNomina concepto = await _catalogos.ObtenerConceptoAsync(comando.ConceptoId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El concepto '{comando.ConceptoId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarConceptoAsync(concepto.Id, cancellationToken);
        await AuditarAsync("Concepto", concepto.Id, concepto.EmpresaId, $"eliminacion; clave={concepto.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExplicacionDeCalculoDto> EjecutarAsync(GuardarExplicacionCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarExplicacionRequest d = comando.Datos;
        ExplicacionDeCalculo explicacion;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ExplicacionId is null)
        {
            explicacion = ExplicacionDeCalculo.Crear(d.Esquema, d.Idioma, d.Orden, d.Titulo, d.Cuerpo, _reloj.GetUtcNow());
            await _catalogos.AgregarExplicacionAsync(explicacion, cancellationToken);
        }
        else
        {
            explicacion = await _catalogos.ObtenerExplicacionAsync(comando.ExplicacionId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"La explicación '{comando.ExplicacionId}' no existe.");
            explicacion.Actualizar(d.Orden, d.Titulo, d.Cuerpo, _reloj.GetUtcNow());
            await _catalogos.ActualizarExplicacionAsync(explicacion, cancellationToken);
        }

        await AuditarAsync("Explicacion", explicacion.Id, null, $"esquema={explicacion.Esquema}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(explicacion);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarExplicacionCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ExplicacionDeCalculo explicacion = await _catalogos.ObtenerExplicacionAsync(comando.ExplicacionId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La explicación '{comando.ExplicacionId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarExplicacionAsync(explicacion.Id, cancellationToken);
        await AuditarAsync("Explicacion", explicacion.Id, null, "eliminacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    private void Exigir() => _autorizador.Exigir(AccionDelSistema.AdministrarCatalogosDeCalculo);

    private Task AuditarAsync(string recurso, Guid id, Guid? empresaId, string detalle, CancellationToken cancellationToken)
        => _auditoria.ExitoAsync(AccionAuditada.AdministracionDeCatalogo, empresaId, recurso, id, detalle, cancellationToken);
}

/// <summary>
/// Ejecuta las consultas de catálogos, la prueba de fórmulas y la explicación completa.
/// </summary>
public sealed class ConsultarCatalogosHandler
    : IManejadorDeConsulta<ListarParametrosQuery, IReadOnlyList<ParametroDeCalculoDto>>,
      IManejadorDeConsulta<ListarTablasQuery, IReadOnlyList<TablaDeRangosDto>>,
      IManejadorDeConsulta<ListarConceptosQuery, IReadOnlyList<ConceptoDeNominaDto>>,
      IManejadorDeConsulta<ProbarFormulaQuery, ProbarFormulaResponse>,
      IManejadorDeConsulta<ListarExplicacionesQuery, IReadOnlyList<ExplicacionDeCalculoDto>>,
      IManejadorDeConsulta<ObtenerExplicacionCompletaQuery, ExplicacionCompletaDto>
{
    private readonly ICatalogoDeCalculoRepository _catalogos;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly MotorDeCalculo _motor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarCatalogosHandler"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    /// <param name="constructor">Resolución del catálogo.</param>
    /// <param name="motor">Motor de cálculo.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public ConsultarCatalogosHandler(
        ICatalogoDeCalculoRepository catalogos,
        ConstructorDePlanDeCalculo constructor,
        MotorDeCalculo motor,
        AutorizadorDeCasosDeUso autorizador,
        TimeProvider reloj)
    {
        _catalogos = catalogos;
        _constructor = constructor;
        _motor = motor;
        _autorizador = autorizador;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ParametroDeCalculoDto>> EjecutarAsync(ListarParametrosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ExigirLectura();
        IReadOnlyList<ParametroDeCalculo> lista = await _catalogos.ListarParametrosAsync(Ambito(consulta.EmpresaId), cancellationToken);
        return lista.Select(Mapeadores.ADto).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TablaDeRangosDto>> EjecutarAsync(ListarTablasQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ExigirLectura();
        IReadOnlyList<TablaDeRangos> lista = await _catalogos.ListarTablasAsync(Ambito(consulta.EmpresaId), cancellationToken);
        return lista.Select(Mapeadores.ADto).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConceptoDeNominaDto>> EjecutarAsync(ListarConceptosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ExigirLectura();
        IReadOnlyList<ConceptoDeNomina> lista = await _catalogos.ListarConceptosAsync(Ambito(consulta.EmpresaId), cancellationToken);
        return lista.Select(Mapeadores.ADto).ToList();
    }

    /// <inheritdoc/>
    public async Task<ProbarFormulaResponse> EjecutarAsync(ProbarFormulaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ArgumentNullException.ThrowIfNull(consulta.Datos);
        ExigirLectura();

        ProbarFormulaRequest d = consulta.Datos;
        FormulaCompilada formula;

        try
        {
            formula = FormulaCompilada.Compilar(d.Formula ?? string.Empty);
        }
        catch (ErrorDeFormulaException excepcion)
        {
            return new ProbarFormulaResponse(false, null, excepcion.Message, [], []);
        }

        var variables = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (DescripcionDeVariable variable in VariablesDeCalculo.Todas)
        {
            variables[variable.Clave] = 0m;
        }

        foreach (ValorDto valor in d.Valores ?? [])
        {
            variables[valor.Clave.Trim().ToUpperInvariant()] = valor.Valor;
        }

        CatalogoResuelto catalogo = await _constructor.ResolverAsync(
            Ambito(d.EmpresaId), DateOnly.FromDateTime(_reloj.GetUtcNow().UtcDateTime), cancellationToken);

        ResultadoDeCalculo previos = ResultadoDeCalculo.Vacio;

        try
        {
            previos = _motor.Calcular(catalogo.Plan(d.Esquema), variables);
        }
        catch (DomainException)
        {
            // El catálogo del esquema puede estar a medio editar: la fórmula se
            // prueba igualmente con las variables y parámetros disponibles.
        }

        var contexto = new ContextoDePrueba(previos, variables, catalogo);

        try
        {
            decimal resultado = formula.Evaluar(contexto);

            return new ProbarFormulaResponse(
                true, resultado, null, formula.Variables.Order(StringComparer.Ordinal).ToList(),
                previos.Valores.Select(static v => new ValorDto(v.Clave, v.Importe)).ToList());
        }
        catch (ErrorDeFormulaException excepcion)
        {
            return new ProbarFormulaResponse(
                false, null, excepcion.Message, formula.Variables.Order(StringComparer.Ordinal).ToList(),
                previos.Valores.Select(static v => new ValorDto(v.Clave, v.Importe)).ToList());
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExplicacionDeCalculoDto>> EjecutarAsync(ListarExplicacionesQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarExplicacionDeCalculos);
        IReadOnlyList<ExplicacionDeCalculo> lista = await _catalogos.ListarExplicacionesAsync(consulta.Esquema, consulta.Idioma, cancellationToken);
        return lista.Select(Mapeadores.ADto).ToList();
    }

    /// <inheritdoc/>
    public async Task<ExplicacionCompletaDto> EjecutarAsync(ObtenerExplicacionCompletaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarExplicacionDeCalculos);

        DateOnly fecha = consulta.Fecha ?? DateOnly.FromDateTime(_reloj.GetUtcNow().UtcDateTime);
        Guid? empresaId = Ambito(consulta.EmpresaId);

        IReadOnlyList<ExplicacionDeCalculo> secciones =
            await _catalogos.ListarExplicacionesAsync(consulta.Esquema, consulta.Idioma, cancellationToken);

        if (secciones.Count == 0 && consulta.Idioma != Idioma.Espanol)
        {
            secciones = await _catalogos.ListarExplicacionesAsync(consulta.Esquema, Idioma.Espanol, cancellationToken);
        }

        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        IReadOnlyList<ConceptoDeNominaDto> conceptos;
        string? error = null;

        try
        {
            conceptos = catalogo.Plan(consulta.Esquema).Conceptos.Select(static c => Mapeadores.ADto(c.Concepto)).ToList();
        }
        catch (DomainException excepcion)
        {
            error = excepcion.Message;
            conceptos = catalogo.Conceptos
                .Where(c => c.Esquemas.Incluye(consulta.Esquema))
                .Select(Mapeadores.ADto)
                .ToList();
        }

        HashSet<string> parametrosUsados = new(
            conceptos.SelectMany(c => FormulaCompilada.Compilar(c.Formula).Variables), StringComparer.Ordinal);

        return new ExplicacionCompletaDto(
            consulta.Esquema,
            consulta.Idioma,
            fecha,
            secciones.OrderBy(static s => s.Orden).Select(Mapeadores.ADto).ToList(),
            conceptos,
            catalogo.ParametrosVigentes
                .Where(p => parametrosUsados.Contains(p.Clave) || ClavesDeParametro.Obligatorias.Contains(p.Clave))
                .OrderBy(static p => p.Grupo).ThenBy(static p => p.Clave, StringComparer.Ordinal)
                .Select(Mapeadores.ADto).ToList(),
            catalogo.TablasVigentes.OrderBy(static t => t.Clave, StringComparer.Ordinal).Select(Mapeadores.ADto).ToList(),
            VariablesDeCalculo.Todas.Select(static v => new VariableDeCalculoDto(v.Clave, v.Descripcion, v.Origen.ToString())).ToList(),
            FuncionesDeFormula.Todas.Select(static f => new FuncionDeFormulaDto(f.Firma, f.Descripcion)).ToList(),
            error);
    }

    private void ExigirLectura()
    {
        if (!_autorizador.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo)
            && !_autorizador.Puede(AccionDelSistema.ConsultarExplicacionDeCalculos))
        {
            throw new AccesoNoAutorizadoException("El usuario no puede consultar los catálogos de cálculo.");
        }
    }

    private Guid? Ambito(Guid? empresaSolicitada)
        => _autorizador.EsTransversal ? empresaSolicitada : _autorizador.ResolverEmpresa(empresaSolicitada);

    /// <summary>
    /// Contexto de evaluación para probar fórmulas: conceptos ya calculados,
    /// variables de ejemplo y catálogo vigente.
    /// </summary>
    private sealed class ContextoDePrueba : IContextoDeEvaluacion
    {
        private readonly ResultadoDeCalculo _previos;
        private readonly IReadOnlyDictionary<string, decimal> _variables;
        private readonly CatalogoResuelto _catalogo;

        public ContextoDePrueba(ResultadoDeCalculo previos, IReadOnlyDictionary<string, decimal> variables, CatalogoResuelto catalogo)
        {
            _previos = previos;
            _variables = variables;
            _catalogo = catalogo;
        }

        public bool TryObtenerValor(string nombre, out decimal valor)
        {
            if (_previos.Contiene(nombre))
            {
                valor = _previos.Obtener(nombre);
                return true;
            }

            return _variables.TryGetValue(nombre, out valor) || _catalogo.Parametros.TryGetValue(nombre, out valor);
        }

        public decimal ConsultarTabla(string tabla, decimal valor, string campo)
        {
            if (!_catalogo.Tablas.TryGetValue(tabla, out TablaDeRangos? definicion))
            {
                throw new ErrorDeFormulaException($"La tabla '{tabla}' no está vigente");
            }

            RangoDeTabla? rango = definicion.Buscar(valor);
            return rango is null ? 0m : TablaDeRangos.Campo(rango, campo);
        }
    }
}
