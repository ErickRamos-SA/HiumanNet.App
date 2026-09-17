namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Fórmula analizada una sola vez y lista para evaluarse cuantas veces haga falta.
/// </summary>
/// <remarks>
/// Compilar es la parte cara (análisis léxico y sintáctico); evaluar es un
/// recorrido del árbol. El motor de cálculo compila las fórmulas del catálogo
/// al construir el plan y las reutiliza para todos los trabajadores del
/// período, lo que mantiene el coste por trabajador en microsegundos.
/// </remarks>
public sealed class FormulaCompilada
{
    private readonly Expresion _raiz;

    /// <summary>
    /// Inicializa una fórmula ya analizada. Sólo la usa <see cref="Compilar"/>.
    /// </summary>
    /// <param name="texto">Texto original.</param>
    /// <param name="raiz">Raíz del árbol de expresión.</param>
    /// <param name="variables">Variables que referencia.</param>
    /// <param name="tablas">Tablas que consulta.</param>
    private FormulaCompilada(string texto, Expresion raiz, HashSet<string> variables, HashSet<string> tablas)
    {
        Texto = texto;
        _raiz = raiz;
        Variables = variables;
        Tablas = tablas;
    }

    /// <summary>
    /// Obtiene el texto original de la fórmula.
    /// </summary>
    /// <value>Tal y como está en el catálogo.</value>
    public string Texto { get; }

    /// <summary>
    /// Obtiene los identificadores (variables, parámetros o conceptos) que la fórmula referencia.
    /// </summary>
    /// <value>Conjunto en mayúsculas; permite ordenar los conceptos por dependencias.</value>
    public IReadOnlySet<string> Variables { get; }

    /// <summary>
    /// Obtiene las claves de tabla que la fórmula consulta.
    /// </summary>
    /// <value>Conjunto en mayúsculas; permite validar que las tablas existen antes de calcular.</value>
    public IReadOnlySet<string> Tablas { get; }

    /// <summary>
    /// Compila una fórmula.
    /// </summary>
    /// <param name="texto">Texto de la fórmula.</param>
    /// <returns>La fórmula compilada.</returns>
    /// <exception cref="Exceptions.ErrorDeFormulaException">
    /// Se lanza si la fórmula tiene errores de sintaxis.
    /// </exception>
    public static FormulaCompilada Compilar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        Expresion raiz = AnalizadorSintactico.Analizar(texto);

        var variables = new HashSet<string>(StringComparer.Ordinal);
        var tablas = new HashSet<string>(StringComparer.Ordinal);
        raiz.RecolectarReferencias(variables, tablas);

        return new FormulaCompilada(texto, raiz, variables, tablas);
    }

    /// <summary>
    /// Evalúa la fórmula contra un contexto.
    /// </summary>
    /// <param name="contexto">Fuente de variables y tablas.</param>
    /// <returns>El resultado numérico.</returns>
    /// <exception cref="Exceptions.ErrorDeFormulaException">
    /// Se lanza si una variable no está definida o un argumento es inválido.
    /// </exception>
    public decimal Evaluar(IContextoDeEvaluacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return _raiz.Evaluar(contexto);
    }

    /// <summary>
    /// Devuelve el texto de la fórmula.
    /// </summary>
    /// <returns>El valor de <see cref="Texto"/>.</returns>
    public override string ToString() => Texto;
}
