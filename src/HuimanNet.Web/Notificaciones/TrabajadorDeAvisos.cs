using System.Globalization;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Notificaciones;
using HuimanNet.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace HuimanNet.Web.Notificaciones;

/// <summary>
/// Consume la cola de avisos y envía los correos correspondientes.
/// </summary>
/// <remarks>
/// Es la contrapartida de <c>PublicadorDeAvisosEnCola</c>: la petición web sólo
/// encola y este servicio en segundo plano hace el trabajo lento. Así un fallo
/// del proveedor de correo nunca hace fracasar una carga de documentos
/// (ARQUITECTURA.md §4.1).
/// <para>
/// Un mensaje sólo se borra de la cola cuando su envío termina bien: si el
/// proceso muere a mitad, el aviso vuelve a estar visible y se reintenta.
/// </para>
/// <para>
/// Quién recibe cada aviso lo decide la capa de aplicación
/// (<see cref="ListarDestinatariosDeAvisoQuery"/>); este servicio sólo lee la
/// cola, redacta el correo y lo envía.
/// </para>
/// </remarks>
public sealed class TrabajadorDeAvisos : BackgroundService
{
    /// <summary>Mensajes que se leen de la cola en cada sondeo.</summary>
    private const int MensajesPorLote = 16;

    private readonly QueueClient _cola;
    private readonly IServiceScopeFactory _fabricaDeAmbitos;
    private readonly IEnviadorDeCorreo _correo;
    private readonly OpcionesDeCorreo _opciones;
    private readonly ILogger<TrabajadorDeAvisos> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TrabajadorDeAvisos"/>.
    /// </summary>
    /// <param name="cola">Cliente de la cola de avisos.</param>
    /// <param name="fabricaDeAmbitos">Fábrica de ámbitos para resolver los casos de uso de cada aviso.</param>
    /// <param name="correo">Enviador de correo.</param>
    /// <param name="opciones">Opciones de correo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public TrabajadorDeAvisos(
        QueueClient cola,
        IServiceScopeFactory fabricaDeAmbitos,
        IEnviadorDeCorreo correo,
        IOptions<OpcionesDeCorreo> opciones,
        ILogger<TrabajadorDeAvisos> logger)
    {
        _cola = cola;
        _fabricaDeAmbitos = fabricaDeAmbitos;
        _correo = correo;
        _opciones = opciones.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trabajador de avisos iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                QueueMessage[] mensajes = await _cola.ReceiveMessagesAsync(
                    MensajesPorLote, cancellationToken: stoppingToken);

                if (mensajes.Length == 0)
                {
                    await Task.Delay(_opciones.IntervaloDeSondeo, stoppingToken);
                    continue;
                }

                foreach (QueueMessage mensaje in mensajes)
                {
                    await ProcesarAsync(mensaje, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // El bucle del trabajador no debe morir por un fallo puntual.
            catch (Exception excepcion)
            {
                _logger.LogError(excepcion, "Fallo al sondear la cola de avisos; se reintentará.");
                await Task.Delay(_opciones.IntervaloDeSondeo, stoppingToken);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation("Trabajador de avisos detenido.");
    }

    /// <summary>
    /// Envía el aviso de un mensaje y lo borra de la cola; si falla, el mensaje
    /// vuelve a aparecer y se reintenta. Los mensajes ilegibles se descartan.
    /// </summary>
    /// <param name="mensaje">Mensaje leído de la cola.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al procesar el mensaje.</returns>
    private async Task ProcesarAsync(QueueMessage mensaje, CancellationToken cancellationToken)
    {
        try
        {
            AvisoPendiente? aviso = JsonSerializer.Deserialize(
                mensaje.Body.ToString(), AvisosJsonContext.Default.AvisoPendiente);

            if (aviso is null)
            {
                _logger.LogWarning("Mensaje de aviso ilegible; se descarta.");
                await _cola.DeleteMessageAsync(mensaje.MessageId, mensaje.PopReceipt, cancellationToken);
                return;
            }

            await EnviarAvisoAsync(aviso, cancellationToken);

            // Sólo se borra tras enviar: si algo falla antes, el aviso se reintenta.
            await _cola.DeleteMessageAsync(mensaje.MessageId, mensaje.PopReceipt, cancellationToken);
        }
#pragma warning disable CA1031 // Un aviso fallido no debe detener el resto del lote.
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion, "No se pudo procesar un aviso; volverá a intentarse al reaparecer en la cola.");
        }
#pragma warning restore CA1031
    }

    /// <summary>Obtiene los destinatarios del aviso y les envía el correo.</summary>
    /// <param name="aviso">Aviso a enviar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al enviar todos los correos.</returns>
    private async Task EnviarAvisoAsync(AvisoPendiente aviso, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope ambito = _fabricaDeAmbitos.CreateAsyncScope();

        IReadOnlyList<string> destinatarios = await ambito.ServiceProvider
            .GetRequiredService<IManejadorDeConsulta<ListarDestinatariosDeAvisoQuery, IReadOnlyList<string>>>()
            .EjecutarAsync(new ListarDestinatariosDeAvisoQuery(aviso.Tipo, aviso.EmpresaId), cancellationToken);

        if (destinatarios.Count == 0)
        {
            _logger.LogWarning(
                "No hay destinatarios con rol {Rol} para el aviso {Tipo}.",
                ListarDestinatariosDeAvisoHandler.RolDestinatario(aviso.Tipo), aviso.Tipo);
            return;
        }

        (string asunto, string cuerpo) = Redactar(aviso);

        foreach (string destinatario in destinatarios)
        {
            await _correo.EnviarAsync(destinatario, asunto, cuerpo, cancellationToken);
        }
    }

    /// <summary>
    /// Redacta el correo de un aviso; sólo incluye el período y un enlace al
    /// portal, nunca nombres de archivo ni datos personales.
    /// </summary>
    /// <param name="aviso">Aviso a enviar.</param>
    /// <returns>El asunto y el cuerpo en texto plano.</returns>
    private (string Asunto, string Cuerpo) Redactar(AvisoPendiente aviso)
    {
        string enlace = $"{_opciones.UrlDelPortal.TrimEnd('/')}/periodos/{aviso.PeriodoId}/documentos";

        // El cuerpo no incluye nombres de archivo ni datos personales: sólo el
        // período y un enlace al portal, donde se aplica la autorización.
        return aviso.Tipo switch
        {
            TipoDeAviso.DocumentosRecibidos => (
                "HuimanNet: nuevos documentos recibidos",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"""
                     Se han recibido {aviso.CantidadDocumentos} documento(s) del período {aviso.DescripcionPeriodo}.

                     Consúltelos en: {enlace}
                     """)),

            TipoDeAviso.ResultadosDisponibles => (
                "HuimanNet: resultados disponibles",
                $"""
                 Ya están disponibles los resultados del período {aviso.DescripcionPeriodo}.

                 Descárguelos en: {enlace}
                 """),

            _ => (
                "HuimanNet: archivo en cuarentena",
                $"""
                 Un archivo cargado en el período {aviso.DescripcionPeriodo} fue puesto en
                 cuarentena porque el análisis antimalware detectó contenido malicioso.

                 Revise el portal: {enlace}
                 """),
        };
    }
}
