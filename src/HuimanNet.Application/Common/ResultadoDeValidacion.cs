namespace HuimanNet.Application.Common;

/// <summary>
/// Resultado de validar la entrada de un caso de uso.
/// </summary>
/// <remarks>
/// Acumula <b>todos</b> los errores en lugar de detenerse en el primero, para
/// que la interfaz pueda señalar de una vez todos los campos a corregir.
/// </remarks>
public sealed class ResultadoDeValidacion
{
    private static readonly ErrorDeValidacion[] SinErrores = [];

    /// <summary>Inicializa un resultado con los errores indicados. Sólo lo usan las fábricas.</summary>
    /// <param name="errores">Errores detectados; vacía si la entrada es válida.</param>
    private ResultadoDeValidacion(IReadOnlyList<ErrorDeValidacion> errores) => Errores = errores;

    /// <summary>
    /// Obtiene los errores detectados.
    /// </summary>
    /// <value>Lista vacía si la entrada es válida.</value>
    public IReadOnlyList<ErrorDeValidacion> Errores { get; }

    /// <summary>
    /// Indica si la entrada superó todas las reglas.
    /// </summary>
    /// <value><c>true</c> si no hay errores.</value>
    public bool EsValido => Errores.Count == 0;

    /// <summary>
    /// Obtiene un resultado sin errores.
    /// </summary>
    /// <value>Instancia compartida que representa una entrada válida.</value>
    public static ResultadoDeValidacion Exitoso { get; } = new(SinErrores);

    /// <summary>
    /// Crea un resultado a partir de una lista de errores.
    /// </summary>
    /// <param name="errores">Errores detectados.</param>
    /// <returns>El resultado correspondiente; exitoso si la lista está vacía.</returns>
    public static ResultadoDeValidacion Con(IReadOnlyList<ErrorDeValidacion> errores)
    {
        ArgumentNullException.ThrowIfNull(errores);
        return errores.Count == 0 ? Exitoso : new ResultadoDeValidacion(errores);
    }

    /// <summary>
    /// Lanza una excepción si la entrada no es válida.
    /// </summary>
    /// <exception cref="EntradaInvalidaException">
    /// Se lanza cuando hay al menos un error de validación.
    /// </exception>
    public void GarantizarValido()
    {
        if (!EsValido)
        {
            throw new EntradaInvalidaException(Errores);
        }
    }
}
