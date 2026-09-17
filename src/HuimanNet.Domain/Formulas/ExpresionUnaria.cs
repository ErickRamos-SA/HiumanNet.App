namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Operación con un solo operando: negación aritmética o lógica.
/// </summary>
public sealed class ExpresionUnaria : Expresion
{
    private readonly Expresion _operando;
    private readonly bool _esNegacionLogica;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionUnaria"/>.
    /// </summary>
    /// <param name="operando">Expresión sobre la que se aplica el operador.</param>
    /// <param name="esNegacionLogica"><c>true</c> para <c>NO</c>; <c>false</c> para el signo negativo.</param>
    public ExpresionUnaria(Expresion operando, bool esNegacionLogica)
    {
        _operando = operando;
        _esNegacionLogica = esNegacionLogica;
    }

    /// <inheritdoc/>
    public override decimal Evaluar(IContextoDeEvaluacion contexto)
    {
        decimal valor = _operando.Evaluar(contexto);
        return _esNegacionLogica ? Logico(!EsVerdadero(valor)) : -valor;
    }

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
        => _operando.RecolectarReferencias(variables, tablas);
}
