namespace HuimanNet.Domain.Exceptions;

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
