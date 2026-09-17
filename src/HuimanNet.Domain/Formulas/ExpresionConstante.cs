namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Literal numérico.
/// </summary>
public sealed class ExpresionConstante : Expresion
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionConstante"/>.
    /// </summary>
    /// <param name="valor">Valor del literal.</param>
    public ExpresionConstante(decimal valor) => Valor = valor;

    /// <summary>
    /// Obtiene el valor del literal.
    /// </summary>
    /// <value>Constante inmutable.</value>
    public decimal Valor { get; }

    /// <inheritdoc/>
    public override decimal Evaluar(IContextoDeEvaluacion contexto) => Valor;

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
    {
    }
}
