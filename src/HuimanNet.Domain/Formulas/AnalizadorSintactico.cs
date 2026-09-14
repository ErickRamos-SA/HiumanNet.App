using System.Globalization;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Analizador sintáctico descendente recursivo del lenguaje de fórmulas.
/// </summary>
/// <remarks>
/// Gramática (de menor a mayor precedencia):
/// <code>
/// expresion   := disyuncion
/// disyuncion  := conjuncion ( "O" conjuncion )*
/// conjuncion  := comparacion ( "Y" comparacion )*
/// comparacion := suma ( ("=" | "&lt;&gt;" | "&lt;" | "&lt;=" | "&gt;" | "&gt;=") suma )?
/// suma        := producto ( ("+" | "-") producto )*
/// producto    := unario ( ("*" | "/") unario )*
/// unario      := ("-" | "+" | "NO") unario | potencia
/// potencia    := primario ( "^" unario )?
/// primario    := numero | cadena | "(" expresion ")" | identificador [ "(" argumentos ")" ]
/// </code>
/// Los operadores lógicos <c>Y</c>, <c>O</c> y <c>NO</c> también se aceptan
/// como <c>AND</c>, <c>OR</c> y <c>NOT</c>.
/// </remarks>
public sealed class AnalizadorSintactico
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _indice;

    private AnalizadorSintactico(IReadOnlyList<Token> tokens) => _tokens = tokens;

    /// <summary>
    /// Analiza una fórmula completa y devuelve su árbol de expresión.
    /// </summary>
    /// <param name="formula">Texto de la fórmula.</param>
    /// <returns>La raíz del árbol.</returns>
    /// <exception cref="ErrorDeFormulaException">
    /// Se lanza si la fórmula está vacía, contiene un error de sintaxis o
    /// invoca una función desconocida o con un número de argumentos incorrecto.
    /// </exception>
    public static Expresion Analizar(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ErrorDeFormulaException("La fórmula está vacía");
        }

        var analizador = new AnalizadorSintactico(AnalizadorLexico.Tokenizar(formula));
        Expresion raiz = analizador.Disyuncion();

        if (analizador.Actual.Tipo != TipoDeToken.Fin)
        {
            throw new ErrorDeFormulaException(
                $"Texto inesperado '{analizador.Actual.Texto}'", analizador.Actual.Posicion);
        }

        return raiz;
    }

    private Token Actual => _tokens[_indice];

    private Token Avanzar() => _tokens[_indice++];

    private bool EsOperador(string texto)
        => Actual.Tipo == TipoDeToken.Operador && Actual.Texto == texto;

    private bool EsPalabra(string palabra)
        => Actual.Tipo == TipoDeToken.Identificador && Actual.Texto == palabra;

    private Expresion Disyuncion()
    {
        Expresion izquierda = Conjuncion();

        while (EsPalabra("O") || EsPalabra("OR"))
        {
            Avanzar();
            izquierda = new ExpresionBinaria(OperadorBinario.O, izquierda, Conjuncion());
        }

        return izquierda;
    }

    private Expresion Conjuncion()
    {
        Expresion izquierda = Comparacion();

        while (EsPalabra("Y") || EsPalabra("AND"))
        {
            Avanzar();
            izquierda = new ExpresionBinaria(OperadorBinario.Y, izquierda, Comparacion());
        }

        return izquierda;
    }

    private Expresion Comparacion()
    {
        Expresion izquierda = Suma();

        if (Actual.Tipo != TipoDeToken.Operador)
        {
            return izquierda;
        }

        OperadorBinario? operador = Actual.Texto switch
        {
            "=" => OperadorBinario.Igual,
            "<>" => OperadorBinario.Distinto,
            "<" => OperadorBinario.Menor,
            "<=" => OperadorBinario.MenorOIgual,
            ">" => OperadorBinario.Mayor,
            ">=" => OperadorBinario.MayorOIgual,
            _ => null,
        };

        if (operador is null)
        {
            return izquierda;
        }

        Avanzar();
        return new ExpresionBinaria(operador.Value, izquierda, Suma());
    }

    private Expresion Suma()
    {
        Expresion izquierda = Producto();

        while (EsOperador("+") || EsOperador("-"))
        {
            OperadorBinario operador = Avanzar().Texto == "+" ? OperadorBinario.Suma : OperadorBinario.Resta;
            izquierda = new ExpresionBinaria(operador, izquierda, Producto());
        }

        return izquierda;
    }

    private Expresion Producto()
    {
        Expresion izquierda = Unario();

        while (EsOperador("*") || EsOperador("/"))
        {
            OperadorBinario operador = Avanzar().Texto == "*"
                ? OperadorBinario.Multiplicacion
                : OperadorBinario.Division;

            izquierda = new ExpresionBinaria(operador, izquierda, Unario());
        }

        return izquierda;
    }

    private Expresion Unario()
    {
        if (EsOperador("-"))
        {
            Avanzar();
            return new ExpresionUnaria(Unario(), esNegacionLogica: false);
        }

        if (EsOperador("+"))
        {
            Avanzar();
            return Unario();
        }

        if ((EsPalabra("NO") || EsPalabra("NOT")) && SiguienteNoEsParentesis())
        {
            Avanzar();
            return new ExpresionUnaria(Unario(), esNegacionLogica: true);
        }

        return Potencia();
    }

    private bool SiguienteNoEsParentesis()
        => _indice + 1 < _tokens.Count && _tokens[_indice + 1].Tipo != TipoDeToken.ParentesisAbre;

    private Expresion Potencia()
    {
        Expresion baseValor = Primario();

        if (EsOperador("^"))
        {
            Avanzar();
            return new ExpresionBinaria(OperadorBinario.Potencia, baseValor, Unario());
        }

        return baseValor;
    }

    private Expresion Primario()
    {
        Token token = Actual;

        switch (token.Tipo)
        {
            case TipoDeToken.Numero:
                Avanzar();
                return new ExpresionConstante(
                    decimal.Parse(token.Texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));

            case TipoDeToken.Cadena:
                Avanzar();
                return new ExpresionCadena(token.Texto, token.Posicion);

            case TipoDeToken.ParentesisAbre:
            {
                Avanzar();
                Expresion interna = Disyuncion();
                Esperar(TipoDeToken.ParentesisCierra, ")");
                return interna;
            }

            case TipoDeToken.Identificador:
                Avanzar();
                return Actual.Tipo == TipoDeToken.ParentesisAbre
                    ? Llamada(token)
                    : new ExpresionVariable(token.Texto, token.Posicion);

            case TipoDeToken.Fin:
                throw new ErrorDeFormulaException("La fórmula termina de forma inesperada", token.Posicion);

            default:
                throw new ErrorDeFormulaException($"Se esperaba un valor y se encontró '{token.Texto}'", token.Posicion);
        }
    }

    private Expresion Llamada(Token nombre)
    {
        if (!FuncionesDeFormula.TryObtener(nombre.Texto, out DescripcionDeFuncion descripcion))
        {
            throw new ErrorDeFormulaException($"Función desconocida '{nombre.Texto}'", nombre.Posicion);
        }

        Esperar(TipoDeToken.ParentesisAbre, "(");

        var argumentos = new List<Expresion>();

        if (Actual.Tipo != TipoDeToken.ParentesisCierra)
        {
            argumentos.Add(Disyuncion());

            while (Actual.Tipo == TipoDeToken.Coma)
            {
                Avanzar();
                argumentos.Add(Disyuncion());
            }
        }

        Esperar(TipoDeToken.ParentesisCierra, ")");

        if (argumentos.Count < descripcion.MinimoArgumentos
            || (descripcion.MaximoArgumentos is { } maximo && argumentos.Count > maximo))
        {
            string esperado = descripcion.MaximoArgumentos is null
                ? $"al menos {descripcion.MinimoArgumentos}"
                : descripcion.MinimoArgumentos == descripcion.MaximoArgumentos
                    ? descripcion.MinimoArgumentos.ToString(CultureInfo.InvariantCulture)
                    : $"entre {descripcion.MinimoArgumentos} y {descripcion.MaximoArgumentos}";

            throw new ErrorDeFormulaException(
                $"La función {nombre.Texto} espera {esperado} argumento(s) y recibió {argumentos.Count}",
                nombre.Posicion);
        }

        return new ExpresionLlamada(descripcion.Funcion, [.. argumentos], nombre.Posicion);
    }

    private void Esperar(TipoDeToken tipo, string texto)
    {
        if (Actual.Tipo != tipo)
        {
            throw new ErrorDeFormulaException(
                $"Se esperaba '{texto}' y se encontró '{(Actual.Tipo == TipoDeToken.Fin ? "fin de fórmula" : Actual.Texto)}'",
                Actual.Posicion);
        }

        Avanzar();
    }
}
