using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta operar sobre un período que ya no admite la
/// operación solicitada por su estado actual.
/// </summary>
public sealed class PeriodoCerradoException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PeriodoCerradoException"/>.
    /// </summary>
    /// <param name="periodoId">Identificador del período afectado.</param>
    /// <param name="estadoActual">Estado en el que se encuentra el período.</param>
    public PeriodoCerradoException(Guid periodoId, EstadoPeriodo estadoActual)
        : base($"El período '{periodoId}' no admite la operación solicitada: su estado actual es '{estadoActual}'.")
    {
        PeriodoId = periodoId;
        EstadoActual = estadoActual;
    }

    /// <inheritdoc/>
    public override string Codigo => "PeriodoCerrado";

    /// <summary>
    /// Obtiene el identificador del período que rechazó la operación.
    /// </summary>
    /// <value>Identificador único del <see cref="Entities.PeriodoCarga"/>.</value>
    public Guid PeriodoId { get; }

    /// <summary>
    /// Obtiene el estado del período en el momento del rechazo.
    /// </summary>
    /// <value>Estado vigente del ciclo de intercambio.</value>
    public EstadoPeriodo EstadoActual { get; }
}
