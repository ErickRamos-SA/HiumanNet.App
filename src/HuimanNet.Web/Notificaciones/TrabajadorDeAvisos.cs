using System.Globalization;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
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
/// </remarks>
public sealed class TrabajadorDeAvisos : BackgroundService
{
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
    /// <param name="fabricaDeAmbitos">Fábrica de ámbitos para resolver repositorios.</param>
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

    private async Task EnviarAvisoAsync(AvisoPendiente aviso, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope ambito = _fabricaDeAmbitos.CreateAsyncScope();

        var usuarios = ambito.ServiceProvider.GetRequiredService<IUsuarioRepository>();

        RolUsuario rolDestino = aviso.Tipo switch
        {
            TipoDeAviso.DocumentosRecibidos => RolUsuario.OperadorNomina,
            TipoDeAviso.ResultadosDisponibles => RolUsuario.ClienteEmpresa,
            _ => RolUsuario.Administrador,
        };

        IReadOnlyList<Usuario> destinatarios =
            await usuarios.ListarDestinatariosAsync(aviso.EmpresaId, rolDestino, cancellationToken);

        if (destinatarios.Count == 0)
        {
            _logger.LogWarning(
                "No hay destinatarios con rol {Rol} para el aviso {Tipo}.", rolDestino, aviso.Tipo);
            return;
        }

        (string asunto, string cuerpo) = Redactar(aviso);

        foreach (Usuario destinatario in destinatarios)
        {
            await _correo.EnviarAsync(destinatario.Correo, asunto, cuerpo, cancellationToken);
        }
    }

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
