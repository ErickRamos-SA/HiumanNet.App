using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Application.Common;

/// <summary>
/// Error concreto detectado al validar la entrada de un caso de uso.
/// </summary>
/// <param name="Campo">Nombre del campo que incumple la regla.</param>
/// <param name="Mensaje">Descripción del incumplimiento, apta para mostrarse al usuario.</param>
public sealed record ErrorDeValidacion(string Campo, string Mensaje);

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

/// <summary>
/// Valida la entrada de un caso de uso antes de que llegue al dominio.
/// </summary>
/// <typeparam name="T">Tipo del comando o consulta a validar.</typeparam>
/// <remarks>
/// Los validadores se escriben a mano y no usan reflexión, requisito para que
/// la API pueda publicarse con Native AOT (ESPECIFICACION.md §6).
/// </remarks>
public interface IValidadorDeEntrada<in T>
{
    /// <summary>
    /// Valida la entrada indicada.
    /// </summary>
    /// <param name="entrada">Comando o consulta a validar.</param>
    /// <returns>El resultado con los errores acumulados.</returns>
    ResultadoDeValidacion Validar(T entrada);
}

/// <summary>
/// Se lanza cuando la entrada de un caso de uso incumple una o varias reglas de
/// validación.
/// </summary>
public sealed class EntradaInvalidaException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EntradaInvalidaException"/>.
    /// </summary>
    /// <param name="errores">Errores acumulados durante la validación.</param>
    public EntradaInvalidaException(IReadOnlyList<ErrorDeValidacion> errores)
        : base("La solicitud contiene datos no válidos.")
        => Errores = errores;

    /// <inheritdoc/>
    public override string Codigo => "EntradaInvalida";

    /// <summary>
    /// Obtiene los errores que provocaron el rechazo.
    /// </summary>
    /// <value>Se proyectan a la sección <c>errors</c> de la respuesta <c>ProblemDetails</c>.</value>
    public IReadOnlyList<ErrorDeValidacion> Errores { get; }
}
