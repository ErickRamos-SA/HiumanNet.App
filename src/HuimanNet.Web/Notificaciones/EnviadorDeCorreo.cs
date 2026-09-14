using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Options;

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

/// <summary>
/// Implementación de <see cref="IEnviadorDeCorreo"/> sobre Azure Communication
/// Services.
/// </summary>
/// <remarks>
/// Vive en el proyecto web y no en <c>HuimanNet.Infrastructure</c> a propósito:
/// la API se publica con Native AOT y no debe enlazar este SDK. La web se
/// compila JIT y es el anfitrión natural del trabajador de avisos.
/// </remarks>
public sealed class EnviadorDeCorreoAcs : IEnviadorDeCorreo
{
    private readonly EmailClient _cliente;
    private readonly OpcionesDeCorreo _opciones;
    private readonly ILogger<EnviadorDeCorreoAcs> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EnviadorDeCorreoAcs"/>.
    /// </summary>
    /// <param name="opciones">Opciones de correo.</param>
    /// <param name="logger">Registro de eventos.</param>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si no hay ni punto de conexión ni cadena de conexión configurados.
    /// </exception>
    public EnviadorDeCorreoAcs(IOptions<OpcionesDeCorreo> opciones, ILogger<EnviadorDeCorreoAcs> logger)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        _opciones = opciones.Value;
        _logger = logger;

        _cliente = !string.IsNullOrWhiteSpace(_opciones.UriServicio)
            ? new EmailClient(new Uri(_opciones.UriServicio), new DefaultAzureCredential())
            : !string.IsNullOrWhiteSpace(_opciones.CadenaDeConexion)
                ? new EmailClient(_opciones.CadenaDeConexion)
                : throw new InvalidOperationException(
                    $"Falta configurar '{OpcionesDeCorreo.Seccion}:{nameof(OpcionesDeCorreo.UriServicio)}' " +
                    $"o '{OpcionesDeCorreo.Seccion}:{nameof(OpcionesDeCorreo.CadenaDeConexion)}'.");
    }

    /// <inheritdoc/>
    public async Task EnviarAsync(
        string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinatario);

        var mensaje = new EmailMessage(
            _opciones.Remitente,
            destinatario,
            new EmailContent(asunto) { PlainText = cuerpo });

        await _cliente.SendAsync(WaitUntil.Started, mensaje, cancellationToken);

        _logger.LogInformation("Aviso enviado por correo a un destinatario del portal.");
    }
}
