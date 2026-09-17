namespace HuimanNet.Domain.Exceptions;

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
