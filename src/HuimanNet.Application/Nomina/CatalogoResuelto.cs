using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;

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
    /// Obtiene un parámetro que el sistema necesita siempre.
    /// </summary>
    /// <param name="clave">Clave del parámetro, de <see cref="ClavesDeParametro"/>.</param>
    /// <returns>El valor vigente.</returns>
    /// <exception cref="Domain.Exceptions.CatalogoInvalidoException">
    /// Se lanza si el catálogo no define el parámetro para la fecha: el
    /// sistema no supone ningún valor.
    /// </exception>
    public decimal ParametroObligatorio(string clave)
        => Parametros.TryGetValue(clave, out decimal valor)
            ? valor
            : throw new Domain.Exceptions.CatalogoInvalidoException(
                $"Falta el parámetro obligatorio '{clave}' en el catálogo para la fecha {Fecha:yyyy-MM-dd}.");

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
