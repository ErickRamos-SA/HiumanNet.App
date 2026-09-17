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

    /// <summary>
    /// Inicializa el analizador sobre los tokens de una fórmula.
    /// </summary>
    /// <param name="tokens">Tokens producidos por <see cref="AnalizadorLexico"/>, terminados en <see cref="TipoDeToken.Fin"/>.</param>
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

    /// <summary>Obtiene el token pendiente de consumir.</summary>
    /// <value>El token en la posición actual.</value>
    private Token Actual => _tokens[_indice];

    /// <summary>Consume el token actual.</summary>
    /// <returns>El token consumido.</returns>
    private Token Avanzar() => _tokens[_indice++];

    /// <summary>Indica si el token actual es el operador indicado.</summary>
    /// <param name="texto">Operador buscado.</param>
    /// <returns><c>true</c> si coincide.</returns>
    private bool EsOperador(string texto)
        => Actual.Tipo == TipoDeToken.Operador && Actual.Texto == texto;

    /// <summary>Indica si el token actual es la palabra reservada indicada.</summary>
    /// <param name="palabra">Palabra en mayúsculas.</param>
    /// <returns><c>true</c> si coincide.</returns>
    private bool EsPalabra(string palabra)
        => Actual.Tipo == TipoDeToken.Identificador && Actual.Texto == palabra;

    /// <summary>Analiza la regla <c>disyuncion</c>: conjunciones unidas por <c>O</c>.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>Analiza la regla <c>conjuncion</c>: comparaciones unidas por <c>Y</c>.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>Analiza la regla <c>comparacion</c>: dos sumas con, a lo sumo, un operador relacional.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>Analiza la regla <c>suma</c>: productos unidos por <c>+</c> o <c>-</c>.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>Analiza la regla <c>producto</c>: unarios unidos por <c>*</c> o <c>/</c>.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>Analiza la regla <c>unario</c>: signo o negación lógica seguidos de otro unario, o una potencia.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>
    /// Indica si al token actual no le sigue un paréntesis, para distinguir el
    /// operador <c>NO x</c> de la función <c>NO(x)</c>.
    /// </summary>
    /// <returns><c>true</c> si el siguiente token no abre paréntesis.</returns>
    private bool SiguienteNoEsParentesis()
        => _indice + 1 < _tokens.Count && _tokens[_indice + 1].Tipo != TipoDeToken.ParentesisAbre;

    /// <summary>Analiza la regla <c>potencia</c>: un primario elevado, opcionalmente, a un unario.</summary>
    /// <returns>El árbol de la subexpresión.</returns>
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

    /// <summary>
    /// Analiza la regla <c>primario</c>: número, texto, expresión entre
    /// paréntesis, variable o llamada a función.
    /// </summary>
    /// <returns>El árbol de la subexpresión.</returns>
    /// <exception cref="ErrorDeFormulaException">Se lanza si la fórmula termina o aparece un token que no es un valor.</exception>
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

    /// <summary>Analiza una llamada a función y comprueba su número de argumentos.</summary>
    /// <param name="nombre">Token con el nombre de la función, ya consumido.</param>
    /// <returns>La expresión de llamada.</returns>
    /// <exception cref="ErrorDeFormulaException">
    /// Se lanza si la función no existe o recibe un número de argumentos fuera de su rango.
    /// </exception>
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

    /// <summary>Consume el token actual si es del tipo esperado.</summary>
    /// <param name="tipo">Tipo de token esperado.</param>
    /// <param name="texto">Representación del token, para el mensaje de error.</param>
    /// <exception cref="ErrorDeFormulaException">Se lanza si el token actual es de otro tipo.</exception>
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
