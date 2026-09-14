using System.Text.Json;
using Azure.Storage.Queues;
using HuimanNet.Application.Interfaces;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Notifications;

/// <summary>
/// Publica los avisos en una cola de Azure Storage.
/// </summary>
/// <remarks>
/// Desacopla la petición web del envío de correo: si el proveedor de correo
/// falla o va lento, la carga de documentos no se ve afectada. El consumo de la
/// cola y el envío corren en un servicio en segundo plano
/// (ARQUITECTURA.md §4.1).
/// </remarks>
public sealed class PublicadorDeAvisosEnCola : INotificationService
{
    private readonly QueueClient _cola;
    private readonly OpcionesDeNotificaciones _opciones;
    private readonly ILogger<PublicadorDeAvisosEnCola> _logger;
    private readonly SemaphoreSlim _cerrojoDeCreacion = new(1, 1);
    private bool _colaVerificada;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PublicadorDeAvisosEnCola"/>.
    /// </summary>
    /// <param name="cola">Cliente de la cola de avisos.</param>
    /// <param name="opciones">Opciones de notificación.</param>
    /// <param name="logger">Registro de eventos.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si alguna dependencia obligatoria es <c>null</c>.
    /// </exception>
    public PublicadorDeAvisosEnCola(
        QueueClient cola,
        IOptions<OpcionesDeNotificaciones> opciones,
        ILogger<PublicadorDeAvisosEnCola> logger)
    {
        ArgumentNullException.ThrowIfNull(cola);
        ArgumentNullException.ThrowIfNull(opciones);

        _cola = cola;
        _opciones = opciones.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task EncolarAsync(
        AvisoPendiente aviso, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aviso);

        await GarantizarColaAsync(cancellationToken);

        string cuerpo = JsonSerializer.Serialize(aviso, AvisosJsonContext.Default.AvisoPendiente);

        await _cola.SendMessageAsync(cuerpo, cancellationToken);

        _logger.LogInformation(
            "Aviso {Tipo} encolado para el período {PeriodoId}.", aviso.Tipo, aviso.PeriodoId);
    }

    private async Task GarantizarColaAsync(CancellationToken cancellationToken)
    {
        if (_colaVerificada || !_opciones.CrearColaAlIniciar)
        {
            return;
        }

        await _cerrojoDeCreacion.WaitAsync(cancellationToken);

        try
        {
            if (!_colaVerificada)
            {
                await _cola.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                _colaVerificada = true;
            }
        }
        finally
        {
            _cerrojoDeCreacion.Release();
        }
    }
}
