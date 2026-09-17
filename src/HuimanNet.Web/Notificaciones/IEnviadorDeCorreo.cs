namespace HuimanNet.Web.Notificaciones;

/// <summary>
/// Envío de correo del portal.
/// </summary>
public interface IEnviadorDeCorreo
{
    /// <summary>
    /// Envía un aviso a un destinatario.
    /// </summary>
    /// <param name="destinatario">Dirección de correo de destino.</param>
    /// <param name="asunto">Asunto del mensaje.</param>
    /// <param name="cuerpo">Cuerpo del mensaje en texto plano.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EnviarAsync(
        string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default);
}
