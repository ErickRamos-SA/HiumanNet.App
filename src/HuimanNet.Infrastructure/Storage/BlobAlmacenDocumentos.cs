using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Storage;

/// <summary>
/// Implementación de <see cref="IAlmacenDocumentos"/> sobre Azure Blob Storage.
/// </summary>
/// <remarks>
/// Emite URL SAS de <b>mínimo privilegio</b>: alcance de un único blob, un solo
/// permiso (lectura <i>o</i> escritura) y vigencia de minutos. El contenedor
/// nunca es público y el servidor nunca transporta el contenido de los archivos
/// (ARQUITECTURA.md §6.2).
/// <para>
/// Cuando la aplicación se autentica con identidad administrada, las firmas se
/// generan con una <i>user delegation key</i> —no hay clave de cuenta que
/// robar—. La clave se renueva con antelación y se comparte entre peticiones,
/// porque obtenerla es una llamada de red.
/// </para>
/// </remarks>
public sealed class BlobAlmacenDocumentos : IAlmacenDocumentos, IDisposable
{
    /// <summary>Vigencia con la que se pide la clave de delegación de usuario.</summary>
    private static readonly TimeSpan VigenciaDeLaClave = TimeSpan.FromHours(1);

    /// <summary>Margen con el que se renueva la clave antes de que caduque.</summary>
    private static readonly TimeSpan MargenDeRenovacion = TimeSpan.FromMinutes(10);

    private readonly BlobServiceClient _servicio;
    private readonly OpcionesDeAlmacenamiento _opciones;
    private readonly TimeProvider _reloj;
    private readonly ILogger<BlobAlmacenDocumentos> _logger;
    private readonly SemaphoreSlim _cerrojoDeClave = new(1, 1);

    private UserDelegationKey? _claveDelegacion;
    private DateTimeOffset _caducidadDeLaClave;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="BlobAlmacenDocumentos"/>.
    /// </summary>
    /// <param name="servicio">Cliente del servicio Blob.</param>
    /// <param name="opciones">Opciones de almacenamiento.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si alguna dependencia obligatoria es <c>null</c>.
    /// </exception>
    public BlobAlmacenDocumentos(
        BlobServiceClient servicio,
        IOptions<OpcionesDeAlmacenamiento> opciones,
        TimeProvider reloj,
        ILogger<BlobAlmacenDocumentos> logger)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(opciones);

        _servicio = servicio;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<EnlaceTemporal> CrearEnlaceDeEscrituraAsync(
        string rutaBlob, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);

        BlobClient blob = ObtenerBlobDeDocumentos(rutaBlob);
        DateTimeOffset expira = _reloj.GetUtcNow().AddMinutes(_opciones.MinutosVigenciaEscritura);

        var constructor = CrearConstructor(
            _opciones.ContenedorDocumentos, rutaBlob, expira);

        // Create + Write: lo mínimo para subir un blob en bloques. Sin lectura,
        // sin borrado y sin listar el contenedor.
        constructor.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        Uri url = await FirmarAsync(blob, constructor, cancellationToken);
        return new EnlaceTemporal(url, expira);
    }

    /// <inheritdoc/>
    public async Task<EnlaceTemporal> CrearEnlaceDeLecturaAsync(
        string rutaBlob, string nombreDescarga, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreDescarga);

        BlobClient blob = ObtenerBlobDeDocumentos(rutaBlob);
        DateTimeOffset expira = _reloj.GetUtcNow().AddMinutes(_opciones.MinutosVigenciaLectura);

        var constructor = CrearConstructor(_opciones.ContenedorDocumentos, rutaBlob, expira);
        constructor.SetPermissions(BlobSasPermissions.Read);

        // Fuerza la descarga con el nombre original en lugar de mostrar el
        // contenido en el navegador: el archivo nunca se interpreta.
        constructor.ContentDisposition = $"attachment; filename=\"{SanearNombre(nombreDescarga)}\"";

        Uri url = await FirmarAsync(blob, constructor, cancellationToken);
        return new EnlaceTemporal(url, expira);
    }

    /// <inheritdoc/>
    public async Task<PropiedadesDeBlob?> ObtenerPropiedadesAsync(
        string rutaBlob, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);

        BlobClient blob = ObtenerBlobDeDocumentos(rutaBlob);

        try
        {
            Response<BlobProperties> respuesta =
                await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

            BlobProperties propiedades = respuesta.Value;

            return new PropiedadesDeBlob(
                propiedades.ContentLength,
                propiedades.ContentHash is null ? null : Convert.ToBase64String(propiedades.ContentHash),
                propiedades.LastModified);
        }
        catch (RequestFailedException excepcion) when (excepcion.Status == 404)
        {
            _logger.LogWarning("No se encontró el blob {RutaBlob} al confirmar la carga.", rutaBlob);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task MoverACuarentenaAsync(
        string rutaBlob, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);

        BlobClient origen = ObtenerBlobDeDocumentos(rutaBlob);
        BlobClient destino = _servicio
            .GetBlobContainerClient(_opciones.ContenedorCuarentena)
            .GetBlobClient(rutaBlob);

        // La copia servidor a servidor necesita leer el origen; se firma un
        // enlace de lectura efímero exclusivamente para esta operación.
        DateTimeOffset expira = _reloj.GetUtcNow().AddMinutes(_opciones.MinutosVigenciaLectura);
        var constructor = CrearConstructor(_opciones.ContenedorDocumentos, rutaBlob, expira);
        constructor.SetPermissions(BlobSasPermissions.Read);

        Uri urlOrigen = await FirmarAsync(origen, constructor, cancellationToken);

        CopyFromUriOperation operacion =
            await destino.StartCopyFromUriAsync(urlOrigen, cancellationToken: cancellationToken);

        await operacion.WaitForCompletionAsync(cancellationToken);
        await origen.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        _logger.LogWarning("Blob {RutaBlob} trasladado al contenedor de cuarentena.", rutaBlob);
    }

    /// <inheritdoc/>
    public async Task<byte[]> LeerAsync(string rutaBlob, long bytesMaximos, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);

        BlobClient blob = ObtenerBlobDeDocumentos(rutaBlob);

        try
        {
            // El tamaño se comprueba antes de descargar para no traer a memoria
            // un archivo que de todos modos se rechazaría.
            Response<BlobProperties> propiedades = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

            if (propiedades.Value.ContentLength > bytesMaximos)
            {
                throw new DocumentoInvalidoException("El archivo excede el tamaño máximo permitido.");
            }

            Response<BlobDownloadResult> descarga = await blob.DownloadContentAsync(cancellationToken);
            return descarga.Value.Content.ToArray();
        }
        catch (RequestFailedException excepcion) when (excepcion.Status == 404)
        {
            _logger.LogWarning("No se encontró el blob {RutaBlob} al leerlo para procesarlo.", rutaBlob);
            throw new DocumentoInvalidoException("El archivo no se encuentra en el almacenamiento.");
        }
    }

    /// <summary>
    /// Libera el semáforo que protege la clave de delegación.
    /// </summary>
    public void Dispose() => _cerrojoDeClave.Dispose();

    /// <summary>Obtiene el cliente de un blob del contenedor de documentos.</summary>
    /// <param name="rutaBlob">Ruta del blob.</param>
    /// <returns>El cliente del blob.</returns>
    private BlobClient ObtenerBlobDeDocumentos(string rutaBlob)
        => _servicio.GetBlobContainerClient(_opciones.ContenedorDocumentos).GetBlobClient(rutaBlob);

    /// <summary>
    /// Prepara una firma SAS de un solo blob, sólo HTTPS y con tolerancia al
    /// desfase de reloj en el inicio.
    /// </summary>
    /// <param name="contenedor">Contenedor del blob.</param>
    /// <param name="rutaBlob">Ruta del blob.</param>
    /// <param name="expira">Instante de expiración.</param>
    /// <returns>El constructor, al que el llamador añade los permisos.</returns>
    private BlobSasBuilder CrearConstructor(string contenedor, string rutaBlob, DateTimeOffset expira)
        => new()
        {
            BlobContainerName = contenedor,
            BlobName = rutaBlob,
            Resource = "b",
            StartsOn = _reloj.GetUtcNow().AddMinutes(-_opciones.MinutosToleranciaDeReloj),
            ExpiresOn = expira,
            Protocol = SasProtocol.Https,
        };

    /// <summary>
    /// Firma la URL del blob: con la clave de cuenta si el cliente la tiene
    /// (Azurite) o con una clave de delegación de usuario (identidad administrada).
    /// </summary>
    /// <param name="blob">Cliente del blob.</param>
    /// <param name="constructor">Firma a emitir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La URL firmada.</returns>
    private async Task<Uri> FirmarAsync(
        BlobClient blob, BlobSasBuilder constructor, CancellationToken cancellationToken)
    {
        // Con clave de cuenta (desarrollo local con Azurite) el propio cliente firma.
        if (blob.CanGenerateSasUri)
        {
            return blob.GenerateSasUri(constructor);
        }

        UserDelegationKey clave = await ObtenerClaveDeDelegacionAsync(cancellationToken);

        return new BlobUriBuilder(blob.Uri)
        {
            Sas = constructor.ToSasQueryParameters(clave, _servicio.AccountName),
        }.ToUri();
    }

    /// <summary>
    /// Obtiene la clave de delegación de usuario; la renueva, con un semáforo,
    /// cuando está por caducar.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La clave vigente.</returns>
    private async Task<UserDelegationKey> ObtenerClaveDeDelegacionAsync(
        CancellationToken cancellationToken)
    {
        DateTimeOffset ahora = _reloj.GetUtcNow();

        if (_claveDelegacion is not null && ahora < _caducidadDeLaClave - MargenDeRenovacion)
        {
            return _claveDelegacion;
        }

        await _cerrojoDeClave.WaitAsync(cancellationToken);

        try
        {
            // Otra petición pudo renovarla mientras se esperaba al semáforo.
            if (_claveDelegacion is not null && ahora < _caducidadDeLaClave - MargenDeRenovacion)
            {
                return _claveDelegacion;
            }

            DateTimeOffset caducidad = ahora.Add(VigenciaDeLaClave);

            Response<UserDelegationKey> respuesta = await _servicio.GetUserDelegationKeyAsync(
                ahora.AddMinutes(-_opciones.MinutosToleranciaDeReloj),
                caducidad,
                cancellationToken);

            _claveDelegacion = respuesta.Value;
            _caducidadDeLaClave = caducidad;

            _logger.LogDebug("Clave de delegación de usuario renovada hasta {Caducidad:o}.", caducidad);

            return _claveDelegacion;
        }
        finally
        {
            _cerrojoDeClave.Release();
        }
    }

    /// <summary>
    /// Quita comillas y saltos de línea del nombre de descarga para que no
    /// rompan la cabecera <c>Content-Disposition</c>.
    /// </summary>
    /// <param name="nombre">Nombre original del archivo.</param>
    /// <returns>El nombre saneado.</returns>
    private static string SanearNombre(string nombre)
        => nombre.Replace("\"", string.Empty, StringComparison.Ordinal)
                 .Replace("\r", string.Empty, StringComparison.Ordinal)
                 .Replace("\n", string.Empty, StringComparison.Ordinal);
}
