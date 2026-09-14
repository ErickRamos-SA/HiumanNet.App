namespace HuimanNet.Domain.Exceptions;

/// <summary>
/// Se lanza cuando un usuario intenta acceder a un recurso que no le corresponde
/// por su rol o por pertenecer a otra empresa cliente.
/// </summary>
/// <remarks>
/// El aislamiento entre empresas es el riesgo número uno de un portal
/// multiempresa (ARQUITECTURA.md §6.2). El mensaje expuesto al cliente debe ser
/// deliberadamente genérico para no revelar la existencia del recurso.
/// </remarks>
public sealed class AccesoNoAutorizadoException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AccesoNoAutorizadoException"/>.
    /// </summary>
    /// <param name="motivo">
    /// Motivo técnico del rechazo. Se registra en la bitácora, <b>no</b> se devuelve al cliente.
    /// </param>
    public AccesoNoAutorizadoException(string motivo)
        : base(motivo)
    {
    }

    /// <inheritdoc/>
    public override string Codigo => "AccesoNoAutorizado";

    /// <summary>
    /// Obtiene el mensaje genérico que sí puede devolverse al cliente.
    /// </summary>
    /// <value>Texto neutro que no confirma ni desmiente la existencia del recurso.</value>
    public static string MensajePublico => "No tiene permisos para acceder al recurso solicitado.";
}
