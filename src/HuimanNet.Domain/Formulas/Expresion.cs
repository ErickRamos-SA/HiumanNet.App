using HuimanNet.Domain.Exceptions;

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

/// <summary>
/// Referencia a una variable, parámetro o concepto por su identificador.
/// </summary>
public sealed class ExpresionVariable : Expresion
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionVariable"/>.
    /// </summary>
    /// <param name="nombre">Identificador en mayúsculas.</param>
    /// <param name="posicion">Posición del identificador en la fórmula.</param>
    public ExpresionVariable(string nombre, int posicion)
    {
        Nombre = nombre;
        Posicion = posicion;
    }

    /// <summary>
    /// Obtiene el identificador referenciado.
    /// </summary>
    /// <value>Nombre en mayúsculas.</value>
    public string Nombre { get; }

    /// <summary>
    /// Obtiene la posición del identificador en la fórmula.
    /// </summary>
    /// <value>Índice base cero.</value>
    public int Posicion { get; }

    /// <inheritdoc/>
    /// <exception cref="ErrorDeFormulaException">Se lanza si el contexto no define el identificador.</exception>
    public override decimal Evaluar(IContextoDeEvaluacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        return contexto.TryObtenerValor(Nombre, out decimal valor)
            ? valor
            : throw new ErrorDeFormulaException($"La variable o parámetro '{Nombre}' no está definido", Posicion);
    }

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
        => variables.Add(Nombre);
}

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

/// <summary>
/// Operadores binarios admitidos por las fórmulas.
/// </summary>
public enum OperadorBinario
{
    /// <summary>Suma.</summary>
    Suma,

    /// <summary>Resta.</summary>
    Resta,

    /// <summary>Multiplicación.</summary>
    Multiplicacion,

    /// <summary>División. Dividir entre cero devuelve cero, como <c>SI.ERROR(x/0;0)</c> en el modelo de referencia.</summary>
    Division,

    /// <summary>Potencia.</summary>
    Potencia,

    /// <summary>Igualdad.</summary>
    Igual,

    /// <summary>Desigualdad.</summary>
    Distinto,

    /// <summary>Menor que.</summary>
    Menor,

    /// <summary>Menor o igual que.</summary>
    MenorOIgual,

    /// <summary>Mayor que.</summary>
    Mayor,

    /// <summary>Mayor o igual que.</summary>
    MayorOIgual,

    /// <summary>Conjunción lógica (<c>Y</c>).</summary>
    Y,

    /// <summary>Disyunción lógica (<c>O</c>).</summary>
    O,
}

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

/// <summary>
/// Llamada a una función incorporada.
/// </summary>
/// <remarks>
/// El repertorio de funciones lo define <see cref="FuncionesDeFormula"/>. Las
/// funciones admiten nombre en español y en inglés (<c>SI</c>/<c>IF</c>,
/// <c>REDONDEAR</c>/<c>ROUND</c>, etc.).
/// </remarks>
public sealed class ExpresionLlamada : Expresion
{
    private readonly FuncionDeFormula _funcion;
    private readonly Expresion[] _argumentos;
    private readonly int _posicion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpresionLlamada"/>.
    /// </summary>
    /// <param name="funcion">Función invocada.</param>
    /// <param name="argumentos">Argumentos en orden.</param>
    /// <param name="posicion">Posición del nombre de la función en la fórmula.</param>
    public ExpresionLlamada(FuncionDeFormula funcion, Expresion[] argumentos, int posicion)
    {
        _funcion = funcion;
        _argumentos = argumentos;
        _posicion = posicion;
    }

    /// <inheritdoc/>
    public override decimal Evaluar(IContextoDeEvaluacion contexto)
    {
        switch (_funcion)
        {
            case FuncionDeFormula.Si:
                return EsVerdadero(_argumentos[0].Evaluar(contexto))
                    ? _argumentos[1].Evaluar(contexto)
                    : _argumentos[2].Evaluar(contexto);

            case FuncionDeFormula.Maximo:
            {
                decimal mejor = _argumentos[0].Evaluar(contexto);

                for (int i = 1; i < _argumentos.Length; i++)
                {
                    decimal v = _argumentos[i].Evaluar(contexto);
                    if (v > mejor)
                    {
                        mejor = v;
                    }
                }

                return mejor;
            }

            case FuncionDeFormula.Minimo:
            {
                decimal mejor = _argumentos[0].Evaluar(contexto);

                for (int i = 1; i < _argumentos.Length; i++)
                {
                    decimal v = _argumentos[i].Evaluar(contexto);
                    if (v < mejor)
                    {
                        mejor = v;
                    }
                }

                return mejor;
            }

            case FuncionDeFormula.Redondear:
                return Math.Round(
                    _argumentos[0].Evaluar(contexto),
                    Decimales(contexto),
                    MidpointRounding.AwayFromZero);

            case FuncionDeFormula.Truncar:
            {
                decimal valor = _argumentos[0].Evaluar(contexto);
                int decimales = Decimales(contexto);
                decimal factor = Escala(decimales);
                return decimal.Truncate(valor * factor) / factor;
            }

            case FuncionDeFormula.Absoluto:
                return Math.Abs(_argumentos[0].Evaluar(contexto));

            case FuncionDeFormula.Entero:
                return decimal.Floor(_argumentos[0].Evaluar(contexto));

            case FuncionDeFormula.Techo:
                return decimal.Ceiling(_argumentos[0].Evaluar(contexto));

            case FuncionDeFormula.Y:
                foreach (Expresion argumento in _argumentos)
                {
                    if (!EsVerdadero(argumento.Evaluar(contexto)))
                    {
                        return 0m;
                    }
                }

                return 1m;

            case FuncionDeFormula.O:
                foreach (Expresion argumento in _argumentos)
                {
                    if (EsVerdadero(argumento.Evaluar(contexto)))
                    {
                        return 1m;
                    }
                }

                return 0m;

            case FuncionDeFormula.No:
                return Logico(!EsVerdadero(_argumentos[0].Evaluar(contexto)));

            case FuncionDeFormula.Entre:
            {
                decimal valor = _argumentos[0].Evaluar(contexto);
                return Logico(valor >= _argumentos[1].Evaluar(contexto) && valor <= _argumentos[2].Evaluar(contexto));
            }

            case FuncionDeFormula.Tabla:
            {
                ArgumentNullException.ThrowIfNull(contexto);

                string tabla = CadenaLiteral(_argumentos[0], "tabla");
                decimal valor = _argumentos[1].Evaluar(contexto);
                string campo = CadenaLiteral(_argumentos[2], "campo");

                return contexto.ConsultarTabla(tabla, valor, campo);
            }

            default:
                throw new ErrorDeFormulaException($"Función no soportada '{_funcion}'", _posicion);
        }
    }

    /// <inheritdoc/>
    public override void RecolectarReferencias(ISet<string> variables, ISet<string> tablas)
    {
        if (_funcion == FuncionDeFormula.Tabla && _argumentos[0] is ExpresionCadena cadena)
        {
            tablas.Add(cadena.Valor);
        }

        foreach (Expresion argumento in _argumentos)
        {
            argumento.RecolectarReferencias(variables, tablas);
        }
    }

    private int Decimales(IContextoDeEvaluacion contexto)
    {
        if (_argumentos.Length < 2)
        {
            return 0;
        }

        decimal valor = _argumentos[1].Evaluar(contexto);

        return valor is < 0 or > 12
            ? throw new ErrorDeFormulaException("El número de decimales debe estar entre 0 y 12", _posicion)
            : (int)valor;
    }

    private static decimal Escala(int decimales)
    {
        decimal factor = 1m;

        for (int i = 0; i < decimales; i++)
        {
            factor *= 10m;
        }

        return factor;
    }

    private string CadenaLiteral(Expresion argumento, string nombre)
        => argumento is ExpresionCadena cadena
            ? cadena.Valor
            : throw new ErrorDeFormulaException(
                $"El argumento '{nombre}' de TABLA debe ser un texto entre comillas", _posicion);
}
