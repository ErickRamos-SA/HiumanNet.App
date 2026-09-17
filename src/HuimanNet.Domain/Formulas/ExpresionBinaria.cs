namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Operación con dos operandos.
/// </summary>
public sealed class ExpresionBinaria : Expresion
{
    private readonly OperadorBinario _operador;
    private readonly Expresion _izquierda;
    private readonly Expresion _derecha;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionBinaria"/>.
    /// </summary>
    /// <param name="operador">Operador a aplicar.</param>
    /// <param name="izquierda">Operando izquierdo.</param>
    /// <param name="derecha">Operando derecho.</param>
    public ExpresionBinaria(OperadorBinario operador, Expresion izquierda, Expresion derecha)
    {
        _operador = operador;
        _izquierda = izquierda;
        _derecha = derecha;
    }

    /// <inheritdoc/>
    public override decimal Evaluar(IContextoDeEvaluacion contexto)
    {
        // Los operadores lógicos cortocircuitan: el operando derecho no se
        // evalúa si el izquierdo ya decide el resultado.
        if (_operador == OperadorBinario.Y)
        {
            return Logico(EsVerdadero(_izquierda.Evaluar(contexto)) && EsVerdadero(_derecha.Evaluar(contexto)));
        }

        if (_operador == OperadorBinario.O)
        {
            return Logico(EsVerdadero(_izquierda.Evaluar(contexto)) || EsVerdadero(_derecha.Evaluar(contexto)));
        }

        decimal a = _izquierda.Evaluar(contexto);
        decimal b = _derecha.Evaluar(contexto);

        return _operador switch
        {
            OperadorBinario.Suma => a + b,
            OperadorBinario.Resta => a - b,
            OperadorBinario.Multiplicacion => a * b,
            OperadorBinario.Division => b == 0m ? 0m : a / b,
            OperadorBinario.Potencia => Potencia(a, b),
            OperadorBinario.Igual => Logico(a == b),
            OperadorBinario.Distinto => Logico(a != b),
            OperadorBinario.Menor => Logico(a < b),
            OperadorBinario.MenorOIgual => Logico(a <= b),
            OperadorBinario.Mayor => Logico(a > b),
            OperadorBinario.MayorOIgual => Logico(a >= b),
            _ => throw new InvalidOperationException($"Operador binario no soportado: {_operador}."),
        };
    }

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
    {
        _izquierda.RecolectarReferencias(variables, tablas);
        _derecha.RecolectarReferencias(variables, tablas);
    }

    /// <summary>
    /// Eleva un valor a una potencia. Con exponente entero (hasta 64) multiplica
    /// en <see cref="decimal"/> para no perder precisión; en otro caso recurre a
    /// <see cref="Math.Pow"/>.
    /// </summary>
    /// <param name="baseValor">Base.</param>
    /// <param name="exponente">Exponente.</param>
    /// <returns>La potencia; cero si se divide entre una base nula.</returns>
    private static decimal Potencia(decimal baseValor, decimal exponente)
    {
        if (exponente == decimal.Truncate(exponente) && Math.Abs(exponente) <= 64)
        {
            decimal resultado = 1m;
            int n = (int)Math.Abs(exponente);

            for (int i = 0; i < n; i++)
            {
                resultado *= baseValor;
            }

            return exponente < 0 ? (resultado == 0m ? 0m : 1m / resultado) : resultado;
        }

        return (decimal)Math.Pow((double)baseValor, (double)exponente);
    }
}
