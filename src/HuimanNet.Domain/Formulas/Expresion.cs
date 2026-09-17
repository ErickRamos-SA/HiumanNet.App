namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Nodo del árbol de sintaxis de una fórmula compilada.
/// </summary>
/// <remarks>
/// Cada nodo sabe evaluarse contra un <see cref="IContextoDeEvaluacion"/>. La
/// evaluación es recursiva y trabaja íntegramente con <see cref="decimal"/>
/// para no introducir errores de coma flotante en importes de nómina.
/// Los valores lógicos se representan como <c>1</c> (verdadero) y <c>0</c> (falso).
/// </remarks>
public abstract class Expresion
{
    /// <summary>
    /// Evalúa el nodo.
    /// </summary>
    /// <param name="contexto">Fuente de variables y tablas.</param>
    /// <returns>El valor numérico del nodo.</returns>
    public abstract decimal Evaluar(IContextoDeEvaluacion contexto);

    /// <summary>
    /// Añade al conjunto los identificadores de variable y las tablas que el nodo referencia.
    /// </summary>
    /// <param name="variables">Acumulador de identificadores de variable.</param>
    /// <param name="tablas">Acumulador de claves de tabla.</param>
    public abstract void RecolectarReferencias(ISet<string> variables, ISet<string> tablas);

    /// <summary>
    /// Convierte un valor lógico a su representación numérica.
    /// </summary>
    /// <param name="valor">Valor lógico.</param>
    /// <returns><c>1</c> si es verdadero; <c>0</c> en caso contrario.</returns>
    protected static decimal Logico(bool valor) => valor ? 1m : 0m;

    /// <summary>
    /// Interpreta un valor numérico como lógico.
    /// </summary>
    /// <param name="valor">Valor numérico.</param>
    /// <returns><c>true</c> si el valor es distinto de cero.</returns>
    protected static bool EsVerdadero(decimal valor) => valor != 0m;
}
