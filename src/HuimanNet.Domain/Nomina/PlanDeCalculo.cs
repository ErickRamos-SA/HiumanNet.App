using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Conjunto cerrado y validado de todo lo que hace falta para calcular un
/// esquema de pago: conceptos ordenados por dependencias, parámetros vigentes
/// y tablas vigentes.
/// </summary>
/// <remarks>
/// Se construye una vez por corrida y se comparte entre todos los trabajadores.
/// La construcción falla temprano si falta un parámetro, una tabla o un
/// concepto referenciado, o si existe una dependencia circular: es preferible
/// rechazar la corrida a producir importes con ceros silenciosos.
/// </remarks>
public sealed class PlanDeCalculo
{
    /// <summary>
    /// Inicializa un plan ya validado. Sólo lo usa <see cref="Construir"/>.
    /// </summary>
    /// <param name="esquema">Esquema de pago que calcula el plan.</param>
    /// <param name="conceptos">Conceptos compilados en orden de evaluación.</param>
    /// <param name="parametros">Parámetros vigentes por clave.</param>
    /// <param name="tablas">Tablas vigentes por clave.</param>
    private PlanDeCalculo(
        EsquemaDePago esquema,
        IReadOnlyList<ConceptoCompilado> conceptos,
        IReadOnlyDictionary<string, decimal> parametros,
        IReadOnlyDictionary<string, TablaDeRangos> tablas)
    {
        Esquema = esquema;
        Conceptos = conceptos;
        Parametros = parametros;
        Tablas = tablas;
    }

    /// <summary>Obtiene el esquema de pago que calcula el plan.</summary>
    /// <value>IMSS, sindicato u honorarios.</value>
    public EsquemaDePago Esquema { get; }

    /// <summary>Obtiene los conceptos en orden de evaluación.</summary>
    /// <value>Orden topológico por dependencias; desempate por <see cref="ConceptoDeNomina.Orden"/>.</value>
    public IReadOnlyList<ConceptoCompilado> Conceptos { get; }

    /// <summary>Obtiene los parámetros vigentes por clave.</summary>
    /// <value>Diccionario en mayúsculas.</value>
    public IReadOnlyDictionary<string, decimal> Parametros { get; }

    /// <summary>Obtiene las tablas vigentes por clave.</summary>
    /// <value>Diccionario en mayúsculas.</value>
    public IReadOnlyDictionary<string, TablaDeRangos> Tablas { get; }

    /// <summary>
    /// Construye y valida el plan de un esquema.
    /// </summary>
    /// <param name="esquema">Esquema de pago a calcular.</param>
    /// <param name="conceptos">Catálogo de conceptos (se filtran los activos que apliquen al esquema).</param>
    /// <param name="parametros">Parámetros vigentes, ya resueltos por empresa y fecha.</param>
    /// <param name="tablas">Tablas vigentes, ya resueltas por empresa y fecha.</param>
    /// <returns>El plan listo para el motor.</returns>
    /// <exception cref="CatalogoInvalidoException">
    /// Se lanza si no hay conceptos para el esquema, si una fórmula referencia
    /// algo que no existe o si hay dependencias circulares.
    /// </exception>
    /// <exception cref="ErrorDeFormulaException">Se lanza si alguna fórmula no compila.</exception>
    public static PlanDeCalculo Construir(
        EsquemaDePago esquema,
        IEnumerable<ConceptoDeNomina> conceptos,
        IReadOnlyDictionary<string, decimal> parametros,
        IReadOnlyDictionary<string, TablaDeRangos> tablas)
    {
        ArgumentNullException.ThrowIfNull(conceptos);
        ArgumentNullException.ThrowIfNull(parametros);
        ArgumentNullException.ThrowIfNull(tablas);

        List<ConceptoCompilado> aplicables = conceptos
            .Where(c => c.Activo && c.Esquemas.Incluye(esquema))
            .OrderBy(static c => c.Orden)
            .ThenBy(static c => c.Clave, StringComparer.Ordinal)
            .Select(static c => new ConceptoCompilado(c, c.Compilar()))
            .ToList();

        if (aplicables.Count == 0)
        {
            throw new CatalogoInvalidoException(
                $"El catálogo no tiene conceptos activos para el esquema '{esquema}'.");
        }

        var porClave = new Dictionary<string, ConceptoCompilado>(StringComparer.Ordinal);

        foreach (ConceptoCompilado concepto in aplicables)
        {
            if (!porClave.TryAdd(concepto.Concepto.Clave, concepto))
            {
                throw new CatalogoInvalidoException(
                    $"La clave de concepto '{concepto.Concepto.Clave}' está duplicada para el esquema '{esquema}'.");
            }
        }

        ValidarReferencias(esquema, aplicables, porClave, parametros, tablas);

        return new PlanDeCalculo(esquema, OrdenarPorDependencias(aplicables, porClave), parametros, tablas);
    }

    /// <summary>
    /// Comprueba que cada variable y tabla que usan las fórmulas exista: otro
    /// concepto, un parámetro vigente, una variable de entrada o una tabla vigente.
    /// </summary>
    /// <param name="esquema">Esquema del plan, para el mensaje de error.</param>
    /// <param name="conceptos">Conceptos aplicables.</param>
    /// <param name="porClave">Los mismos conceptos indexados por clave.</param>
    /// <param name="parametros">Parámetros vigentes.</param>
    /// <param name="tablas">Tablas vigentes.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza con todas las referencias que faltan.</exception>
    private static void ValidarReferencias(
        EsquemaDePago esquema,
        List<ConceptoCompilado> conceptos,
        Dictionary<string, ConceptoCompilado> porClave,
        IReadOnlyDictionary<string, decimal> parametros,
        IReadOnlyDictionary<string, TablaDeRangos> tablas)
    {
        var faltantes = new List<string>();

        foreach (ConceptoCompilado concepto in conceptos)
        {
            foreach (string referencia in concepto.Formula.Variables)
            {
                if (!porClave.ContainsKey(referencia)
                    && !parametros.ContainsKey(referencia)
                    && !VariablesDeCalculo.EsVariable(referencia))
                {
                    faltantes.Add($"'{referencia}' (usado por {concepto.Concepto.Clave})");
                }
            }

            foreach (string tabla in concepto.Formula.Tablas)
            {
                if (!tablas.ContainsKey(tabla))
                {
                    faltantes.Add($"tabla '{tabla}' (usada por {concepto.Concepto.Clave})");
                }
            }
        }

        if (faltantes.Count > 0)
        {
            throw new CatalogoInvalidoException(
                $"El catálogo del esquema '{esquema}' referencia elementos que no existen o no están vigentes: "
                + string.Join(", ", faltantes.Distinct(StringComparer.Ordinal)) + ".");
        }
    }

    /// <summary>
    /// Ordena los conceptos de forma que cada uno se evalúe después de los que referencia.
    /// </summary>
    /// <remarks>
    /// Algoritmo de Kahn con desempate estable por el orden de presentación,
    /// de modo que dos catálogos equivalentes producen siempre la misma
    /// secuencia y los resultados son reproducibles.
    /// </remarks>
    /// <param name="conceptos">Conceptos aplicables, en orden de presentación.</param>
    /// <param name="porClave">Los mismos conceptos indexados por clave.</param>
    /// <returns>Los conceptos en orden de evaluación.</returns>
    /// <exception cref="CatalogoInvalidoException">
    /// Se lanza si un concepto se referencia a sí mismo o hay una dependencia circular.
    /// </exception>
    private static IReadOnlyList<ConceptoCompilado> OrdenarPorDependencias(
        List<ConceptoCompilado> conceptos, Dictionary<string, ConceptoCompilado> porClave)
    {
        var pendientes = new Dictionary<string, int>(StringComparer.Ordinal);
        var dependientes = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (ConceptoCompilado concepto in conceptos)
        {
            string clave = concepto.Concepto.Clave;
            int dependencias = 0;

            foreach (string referencia in concepto.Formula.Variables)
            {
                if (referencia == clave)
                {
                    throw new CatalogoInvalidoException($"El concepto '{clave}' se referencia a sí mismo.");
                }

                if (!porClave.ContainsKey(referencia))
                {
                    continue;
                }

                dependencias++;

                if (!dependientes.TryGetValue(referencia, out List<string>? lista))
                {
                    lista = [];
                    dependientes[referencia] = lista;
                }

                lista.Add(clave);
            }

            pendientes[clave] = dependencias;
        }

        // Los conceptos listos se toman siempre en orden de presentación.
        var listos = new SortedSet<ConceptoCompilado>(ComparadorDeOrden.Instancia);

        foreach (ConceptoCompilado concepto in conceptos)
        {
            if (pendientes[concepto.Concepto.Clave] == 0)
            {
                listos.Add(concepto);
            }
        }

        var resultado = new List<ConceptoCompilado>(conceptos.Count);

        while (listos.Count > 0)
        {
            ConceptoCompilado actual = listos.Min!;
            listos.Remove(actual);
            resultado.Add(actual);

            if (!dependientes.TryGetValue(actual.Concepto.Clave, out List<string>? hijos))
            {
                continue;
            }

            foreach (string hijo in hijos)
            {
                if (--pendientes[hijo] == 0)
                {
                    listos.Add(porClave[hijo]);
                }
            }
        }

        if (resultado.Count != conceptos.Count)
        {
            IEnumerable<string> enCiclo = pendientes.Where(static p => p.Value > 0).Select(static p => p.Key);

            throw new CatalogoInvalidoException(
                "Hay una dependencia circular entre conceptos: " + string.Join(", ", enCiclo) + ".");
        }

        return resultado;
    }

    /// <summary>
    /// Ordena conceptos por <see cref="ConceptoDeNomina.Orden"/> y, a igualdad,
    /// por clave, para que el orden de evaluación sea estable.
    /// </summary>
    private sealed class ComparadorDeOrden : IComparer<ConceptoCompilado>
    {
        /// <summary>Instancia única, sin estado.</summary>
        public static readonly ComparadorDeOrden Instancia = new();

        /// <inheritdoc/>
        public int Compare(ConceptoCompilado? x, ConceptoCompilado? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            int porOrden = x.Concepto.Orden.CompareTo(y.Concepto.Orden);
            return porOrden != 0
                ? porOrden
                : string.CompareOrdinal(x.Concepto.Clave, y.Concepto.Clave);
        }
    }
}
