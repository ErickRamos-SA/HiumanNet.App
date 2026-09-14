namespace HuimanNet.Domain.Exceptions;

/// <summary>
/// Excepción base de todas las violaciones de reglas de negocio del dominio.
/// </summary>
/// <remarks>
/// El borde HTTP la traduce a una respuesta <c>ProblemDetails</c> (RFC 7807);
/// consulte el manejador global de excepciones de <c>HuimanNet.Api</c>.
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DomainException"/>.
    /// </summary>
    /// <param name="mensaje">Descripción de la regla de negocio violada.</param>
    protected DomainException(string mensaje)
        : base(mensaje)
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DomainException"/> con una causa subyacente.
    /// </summary>
    /// <param name="mensaje">Descripción de la regla de negocio violada.</param>
    /// <param name="innerException">Excepción que originó ésta.</param>
    protected DomainException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }

    /// <summary>
    /// Obtiene el código estable que identifica el tipo de error para los clientes.
    /// </summary>
    /// <value>Cadena en <c>PascalCase</c>, por ejemplo <c>"DocumentoInvalido"</c>.</value>
    public abstract string Codigo { get; }
}
