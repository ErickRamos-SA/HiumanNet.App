namespace HuimanNet.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una fórmula del catálogo de cálculo no puede compilarse o
/// evaluarse: error de sintaxis, función desconocida, variable no definida o
/// argumentos incorrectos.
/// </summary>
/// <remarks>
/// El mensaje incluye la posición del error dentro de la fórmula para que el
/// administrador pueda corregirla desde la interfaz sin intervención técnica.
/// </remarks>
public sealed class ErrorDeFormulaException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ErrorDeFormulaException"/>.
    /// </summary>
    /// <param name="mensaje">Descripción del problema.</param>
    /// <param name="posicion">Posición (base cero) dentro de la fórmula, o <c>-1</c> si no aplica.</param>
    public ErrorDeFormulaException(string mensaje, int posicion = -1)
        : base(posicion >= 0 ? $"{mensaje} (posición {posicion + 1})." : mensaje)
        => Posicion = posicion;

    /// <inheritdoc/>
    public override string Codigo => "ErrorDeFormula";

    /// <summary>
    /// Obtiene la posición del error dentro del texto de la fórmula.
    /// </summary>
    /// <value>Índice base cero, o <c>-1</c> cuando el error no está ligado a una posición.</value>
    public int Posicion { get; }
}

/// <summary>
/// Se lanza cuando un catálogo del cálculo (parámetros, tablas o conceptos)
/// está incompleto o es inconsistente para el período que se pretende calcular.
/// </summary>
public sealed class CatalogoInvalidoException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CatalogoInvalidoException"/>.
    /// </summary>
    /// <param name="mensaje">Descripción de la inconsistencia, apta para mostrarse al administrador.</param>
    public CatalogoInvalidoException(string mensaje)
        : base(mensaje)
    {
    }

    /// <inheritdoc/>
    public override string Codigo => "CatalogoInvalido";
}

/// <summary>
/// Se lanza cuando una operación de nómina no puede ejecutarse por el estado
/// de los datos: período sin incidencias, contrato inactivo, corrida en un
/// estado que no admite la transición, etc.
/// </summary>
public sealed class NominaInvalidaException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="NominaInvalidaException"/>.
    /// </summary>
    /// <param name="mensaje">Motivo del rechazo, apto para mostrarse al usuario.</param>
    public NominaInvalidaException(string mensaje)
        : base(mensaje)
    {
    }

    /// <inheritdoc/>
    public override string Codigo => "NominaInvalida";
}
