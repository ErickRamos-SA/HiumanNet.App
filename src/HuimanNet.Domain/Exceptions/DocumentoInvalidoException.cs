namespace HuimanNet.Domain.Exceptions;

/// <summary>
/// Se lanza cuando un documento no cumple la política de carga: extensión no
/// permitida, tamaño fuera de límites o nombre de archivo inaceptable.
/// </summary>
public sealed class DocumentoInvalidoException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DocumentoInvalidoException"/>.
    /// </summary>
    /// <param name="mensaje">Motivo concreto del rechazo, apto para mostrarse al usuario.</param>
    public DocumentoInvalidoException(string mensaje)
        : base(mensaje)
    {
    }

    /// <inheritdoc/>
    public override string Codigo => "DocumentoInvalido";
}
