using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Catálogo de cálculo ya resuelto para una empresa y una fecha: parámetros y
/// tablas vigentes con la sobrescritura de empresa aplicada, y conceptos
/// efectivos.
/// </summary>
/// <remarks>
/// Los planes por esquema se construyen bajo demanda y se memorizan: una
/// corrida con contratos IMSS y sindicales construye dos planes, no uno por
/// trabajador.
/// </remarks>
public sealed class CatalogoResuelto
{
    private readonly Dictionary<EsquemaDePago, PlanDeCalculo> _planes = new();
    private readonly object _cerrojo = new();

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CatalogoResuelto"/>.
    /// </summary>
    /// <param name="fecha">Fecha de referencia.</param>
    /// <param name="parametros">Parámetros vigentes efectivos.</param>
    /// <param name="tablas">Tablas vigentes efectivas.</param>
    /// <param name="conceptos">Conceptos efectivos (globales sobrescritos por los de la empresa).</param>
    public CatalogoResuelto(
        DateOnly fecha,
        IReadOnlyList<ParametroDeCalculo> parametros,
        IReadOnlyList<TablaDeRangos> tablas,
        IReadOnlyList<ConceptoDeNomina> conceptos)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        ArgumentNullException.ThrowIfNull(tablas);
        ArgumentNullException.ThrowIfNull(conceptos);

        Fecha = fecha;
        ParametrosVigentes = parametros;
        TablasVigentes = tablas;
        Conceptos = conceptos;
        Parametros = parametros.ToDictionary(static p => p.Clave, static p => p.Valor, StringComparer.Ordinal);
        Tablas = tablas.ToDictionary(static t => t.Clave, static t => t, StringComparer.Ordinal);
    }

    /// <summary>Obtiene la fecha de referencia.</summary>
    /// <value>Fecha con la que se resolvieron las vigencias.</value>
    public DateOnly Fecha { get; }

    /// <summary>Obtiene los parámetros vigentes, uno por clave.</summary>
    /// <value>Lista de sólo lectura.</value>
    public IReadOnlyList<ParametroDeCalculo> ParametrosVigentes { get; }

    /// <summary>Obtiene las tablas vigentes, una por clave.</summary>
    /// <value>Lista de sólo lectura.</value>
    public IReadOnlyList<TablaDeRangos> TablasVigentes { get; }

    /// <summary>Obtiene los conceptos efectivos.</summary>
    /// <value>Lista de sólo lectura, incluidos los inactivos.</value>
    public IReadOnlyList<ConceptoDeNomina> Conceptos { get; }

    /// <summary>Obtiene los valores de los parámetros por clave.</summary>
    /// <value>Diccionario en mayúsculas.</value>
    public IReadOnlyDictionary<string, decimal> Parametros { get; }

    /// <summary>Obtiene las tablas por clave.</summary>
    /// <value>Diccionario en mayúsculas.</value>
    public IReadOnlyDictionary<string, TablaDeRangos> Tablas { get; }

    /// <summary>
    /// Obtiene el plan de un esquema, construyéndolo la primera vez.
    /// </summary>
    /// <param name="esquema">Esquema de pago.</param>
    /// <returns>El plan validado.</returns>
    /// <exception cref="Domain.Exceptions.CatalogoInvalidoException">Se lanza si el catálogo del esquema es inconsistente.</exception>
    public PlanDeCalculo Plan(EsquemaDePago esquema)
    {
        lock (_cerrojo)
        {
            if (!_planes.TryGetValue(esquema, out PlanDeCalculo? plan))
            {
                plan = PlanDeCalculo.Construir(esquema, Conceptos, Parametros, Tablas);
                _planes[esquema] = plan;
            }

            return plan;
        }
    }
}

/// <summary>
/// Carga los catálogos y resuelve vigencias y sobrescrituras por empresa.
/// </summary>
/// <remarks>
/// Reglas de resolución, por clave:
/// <list type="number">
///   <item><description>Sólo cuentan las entradas vigentes en la fecha.</description></item>
///   <item><description>La entrada de la empresa prevalece sobre la global.</description></item>
///   <item><description>Entre varias vigentes del mismo ámbito gana la de inicio de vigencia más reciente.</description></item>
/// </list>
/// Para los conceptos no hay vigencia: la entrada de la empresa sustituye a la
/// global con la misma clave, y si está inactiva el concepto queda excluido
/// para esa empresa.
/// </remarks>
public sealed class ConstructorDePlanDeCalculo
{
    private readonly ICatalogoDeCalculoRepository _catalogos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConstructorDePlanDeCalculo"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    public ConstructorDePlanDeCalculo(ICatalogoDeCalculoRepository catalogos) => _catalogos = catalogos;

    /// <summary>
    /// Resuelve el catálogo de una empresa para una fecha.
    /// </summary>
    /// <param name="empresaId">Empresa, o <c>null</c> para el catálogo global.</param>
    /// <param name="fecha">Fecha de referencia.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El catálogo resuelto.</returns>
    public async Task<CatalogoResuelto> ResolverAsync(
        Guid? empresaId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ParametroDeCalculo> parametros = await _catalogos.ListarParametrosAsync(empresaId, cancellationToken);
        IReadOnlyList<TablaDeRangos> tablas = await _catalogos.ListarTablasAsync(empresaId, cancellationToken);
        IReadOnlyList<ConceptoDeNomina> conceptos = await _catalogos.ListarConceptosAsync(empresaId, cancellationToken);

        return new CatalogoResuelto(
            fecha,
            ResolverVigentes(parametros, empresaId, fecha, static p => p.Clave, static p => p.EmpresaId, static p => p.EstaVigenteEn, static p => p.VigenteDesde),
            ResolverVigentes(tablas, empresaId, fecha, static t => t.Clave, static t => t.EmpresaId, static t => t.EstaVigenteEn, static t => t.VigenteDesde),
            ResolverConceptos(conceptos, empresaId));
    }

    private static IReadOnlyList<T> ResolverVigentes<T>(
        IReadOnlyList<T> entradas,
        Guid? empresaId,
        DateOnly fecha,
        Func<T, string> clave,
        Func<T, Guid?> empresa,
        Func<T, Func<DateOnly, bool>> vigente,
        Func<T, DateOnly> desde)
    {
        var elegidos = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (T entrada in entradas)
        {
            Guid? ambito = empresa(entrada);

            if (ambito is not null && ambito != empresaId)
            {
                continue;
            }

            if (!vigente(entrada)(fecha))
            {
                continue;
            }

            string k = clave(entrada);

            if (!elegidos.TryGetValue(k, out T? actual) || EsMejor(entrada, actual, empresa, desde))
            {
                elegidos[k] = entrada;
            }
        }

        return [.. elegidos.Values];
    }

    private static bool EsMejor<T>(T candidato, T actual, Func<T, Guid?> empresa, Func<T, DateOnly> desde)
    {
        bool candidatoDeEmpresa = empresa(candidato) is not null;
        bool actualDeEmpresa = empresa(actual) is not null;

        if (candidatoDeEmpresa != actualDeEmpresa)
        {
            return candidatoDeEmpresa;
        }

        return desde(candidato) > desde(actual);
    }

    private static IReadOnlyList<ConceptoDeNomina> ResolverConceptos(IReadOnlyList<ConceptoDeNomina> conceptos, Guid? empresaId)
    {
        // Un mismo concepto (por ejemplo TOTAL_PERCEPCIONES) puede tener una
        // definición distinta por esquema; la identidad es clave + esquemas.
        var elegidos = new Dictionary<(string Clave, EsquemasDePago Esquemas), ConceptoDeNomina>();

        foreach (ConceptoDeNomina concepto in conceptos)
        {
            if (concepto.EmpresaId is not null && concepto.EmpresaId != empresaId)
            {
                continue;
            }

            (string Clave, EsquemasDePago Esquemas) identidad = (concepto.Clave, concepto.Esquemas);

            if (!elegidos.TryGetValue(identidad, out ConceptoDeNomina? actual)
                || (concepto.EmpresaId is not null && actual.EmpresaId is null))
            {
                elegidos[identidad] = concepto;
            }
        }

        return [.. elegidos.Values.OrderBy(static c => c.Orden).ThenBy(static c => c.Clave, StringComparer.Ordinal)];
    }
}
