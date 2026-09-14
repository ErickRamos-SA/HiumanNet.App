using Azure.Storage.Blobs;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Storage;

/// <summary>
/// Crea los contenedores de documentos y de cuarentena si no existen.
/// </summary>
/// <remarks>
/// Pensado para desarrollo local con Azurite. En Azure los contenedores los
/// aprovisiona la plantilla de Bicep, con su configuración de versionado,
/// borrado temporal y retención; dejar que los cree la aplicación se perdería
/// esa configuración.
/// </remarks>
public sealed class InicializadorDeAlmacenamiento
{
    private readonly BlobServiceClient _servicio;
    private readonly OpcionesDeAlmacenamiento _opciones;
    private readonly ILogger<InicializadorDeAlmacenamiento> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="InicializadorDeAlmacenamiento"/>.
    /// </summary>
    /// <param name="servicio">Cliente del servicio Blob.</param>
    /// <param name="opciones">Opciones de almacenamiento.</param>
    /// <param name="logger">Registro de eventos.</param>
    public InicializadorDeAlmacenamiento(
        BlobServiceClient servicio,
        IOptions<OpcionesDeAlmacenamiento> opciones,
        ILogger<InicializadorDeAlmacenamiento> logger)
    {
        _servicio = servicio;
        _opciones = opciones.Value;
        _logger = logger;
    }

    /// <summary>
    /// Crea los contenedores que falten, siempre como privados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task GarantizarContenedoresAsync(CancellationToken cancellationToken = default)
    {
        foreach (string contenedor in new[] { _opciones.ContenedorDocumentos, _opciones.ContenedorCuarentena })
        {
            BlobContainerClient cliente = _servicio.GetBlobContainerClient(contenedor);

            // PublicAccessType.None es deliberado: el acceso siempre es por SAS.
            await cliente.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Contenedor {Contenedor} verificado.", contenedor);
        }
    }
}
