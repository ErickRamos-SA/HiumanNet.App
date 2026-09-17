using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Application.Common;

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
