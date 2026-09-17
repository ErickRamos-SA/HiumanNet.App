namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Publicación de avisos hacia el canal de notificación.
/// </summary>
/// <remarks>
/// La petición web sólo <b>encola</b>: el envío del correo lo realiza un
/// <c>BackgroundService</c> aparte, de modo que un fallo del proveedor de correo
/// nunca hace fracasar una carga de documentos (ARQUITECTURA.md §4.1).
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Encola un aviso para su envío asíncrono.
    /// </summary>
    /// <param name="aviso">Aviso a publicar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EncolarAsync(AvisoPendiente aviso, CancellationToken cancellationToken = default);
}
