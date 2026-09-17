using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Formulas;

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
