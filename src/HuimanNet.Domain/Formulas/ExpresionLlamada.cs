using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Formulas;

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

    /// <summary>
    /// Evalúa el segundo argumento opcional de <c>REDONDEAR</c> y <c>TRUNCAR</c>.
    /// </summary>
    /// <param name="contexto">Contexto de evaluación.</param>
    /// <returns>El número de decimales; cero si no se indicó.</returns>
    /// <exception cref="ErrorDeFormulaException">Se lanza si no está entre 0 y 12.</exception>
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

    /// <summary>Calcula diez elevado al número de decimales, sin pasar por <see cref="double"/>.</summary>
    /// <param name="decimales">Número de decimales.</param>
    /// <returns>El factor de escala.</returns>
    private static decimal Escala(int decimales)
    {
        decimal factor = 1m;

        for (int i = 0; i < decimales; i++)
        {
            factor *= 10m;
        }

        return factor;
    }

    /// <summary>Lee un argumento de <c>TABLA</c> que debe ser un texto entre comillas.</summary>
    /// <param name="argumento">Argumento de la llamada.</param>
    /// <param name="nombre">Nombre del argumento, para el mensaje de error.</param>
    /// <returns>El texto literal.</returns>
    /// <exception cref="ErrorDeFormulaException">Se lanza si el argumento no es un texto literal.</exception>
    private string CadenaLiteral(Expresion argumento, string nombre)
        => argumento is ExpresionCadena cadena
            ? cadena.Valor
            : throw new ErrorDeFormulaException(
                $"El argumento '{nombre}' de TABLA debe ser un texto entre comillas", _posicion);
}
