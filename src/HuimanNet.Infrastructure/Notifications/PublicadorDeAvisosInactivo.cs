using HuimanNet.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Infrastructure.Notifications;

/// <summary>
/// Implementación de <see cref="INotificationService"/> que registra el aviso y
/// lo descarta.
/// </summary>
/// <remarks>
/// Se activa cuando el canal de avisos está deshabilitado por configuración.
/// Permite levantar el portal completo en desarrollo o en pruebas de integración
/// sin depender de una cuenta de almacenamiento.
/// </remarks>
public sealed class PublicadorDeAvisosInactivo : INotificationService
{
    private readonly ILogger<PublicadorDeAvisosInactivo> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PublicadorDeAvisosInactivo"/>.
    /// </summary>
    /// <param name="logger">Registro de eventos.</param>
    public PublicadorDeAvisosInactivo(ILogger<PublicadorDeAvisosInactivo> logger) => _logger = logger;

    /// <inheritdoc/>
    public Task EncolarAsync(AvisoPendiente aviso, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aviso);

        _logger.LogInformation(
            "Canal de avisos deshabilitado: se descarta el aviso {Tipo} del período {PeriodoId}.",
            aviso.Tipo, aviso.PeriodoId);

        return Task.CompletedTask;
    }
}
