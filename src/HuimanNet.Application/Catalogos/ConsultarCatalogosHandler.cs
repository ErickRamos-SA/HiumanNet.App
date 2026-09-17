using HuimanNet.Application.Common;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Catalogos;

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

    /// <summary>
    /// Exige poder consultar los catálogos: administrarlos o consultar la
    /// explicación de los cálculos.
    /// </summary>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el usuario no tiene ninguno de los dos permisos.</exception>
    private void ExigirLectura()
    {
        if (!_autorizador.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo)
            && !_autorizador.Puede(AccionDelSistema.ConsultarExplicacionDeCalculos))
        {
            throw new AccesoNoAutorizadoException("El usuario no puede consultar los catálogos de cálculo.");
        }
    }

    /// <summary>Resuelve la empresa cuyo catálogo se consulta.</summary>
    /// <param name="empresaSolicitada">Empresa indicada en la petición.</param>
    /// <returns>
    /// Para nómina y administración, la indicada (<c>null</c> es el catálogo
    /// general); para la empresa cliente, la suya.
    /// </returns>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si una empresa cliente pide otra empresa.</exception>
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

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ContextoDePrueba"/>.
        /// </summary>
        /// <param name="previos">Conceptos del catálogo ya calculados con las variables de ejemplo.</param>
        /// <param name="variables">Variables de ejemplo.</param>
        /// <param name="catalogo">Parámetros y tablas vigentes.</param>
        public ContextoDePrueba(ResultadoDeCalculo previos, IReadOnlyDictionary<string, decimal> variables, CatalogoResuelto catalogo)
        {
            _previos = previos;
            _variables = variables;
            _catalogo = catalogo;
        }

        /// <inheritdoc/>
        public bool TryObtenerValor(string nombre, out decimal valor)
        {
            if (_previos.Contiene(nombre))
            {
                valor = _previos.Obtener(nombre);
                return true;
            }

            return _variables.TryGetValue(nombre, out valor) || _catalogo.Parametros.TryGetValue(nombre, out valor);
        }

        /// <inheritdoc/>
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
