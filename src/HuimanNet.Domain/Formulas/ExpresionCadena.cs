using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Literal de texto. Sólo es válido como argumento de la función <c>TABLA</c>.
/// </summary>
public sealed class ExpresionCadena : Expresion
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionCadena"/>.
    /// </summary>
    /// <param name="valor">Texto del literal, en mayúsculas.</param>
    /// <param name="posicion">Posición del literal en la fórmula.</param>
    public ExpresionCadena(string valor, int posicion)
    {
        Valor = valor;
        Posicion = posicion;
    }

    /// <summary>
    /// Obtiene el texto del literal.
    /// </summary>
    /// <value>Cadena en mayúsculas.</value>
    public string Valor { get; }

    /// <summary>
    /// Obtiene la posición del literal en la fórmula.
    /// </summary>
    /// <value>Índice base cero.</value>
    public int Posicion { get; }

    /// <inheritdoc/>
    /// <exception cref="ErrorDeFormulaException">Se lanza siempre: una cadena no tiene valor numérico.</exception>
    public override decimal Evaluar(IContextoDeEvaluacion contexto)
        => throw new ErrorDeFormulaException(
            $"La cadena \"{Valor}\" sólo puede usarse como argumento de TABLA", Posicion);

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
    {
    }
}
