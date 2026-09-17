using HuimanNet.Application.Interfaces;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Storage;

/// <summary>
/// Implementación de <see cref="IAlmacenDocumentos"/> sobre el disco local.
/// </summary>
/// <remarks>
/// Pensada para desarrollo. Para desplegar en Azure se cambia
/// <c>Almacenamiento:Proveedor</c> a <c>AzureBlob</c>: ningún caso de uso ni
/// cliente cambia, porque ambos proveedores emiten enlaces firmados equivalentes.
/// </remarks>
public sealed class AlmacenLocalDeDocumentos : IAlmacenDocumentos
{
    private readonly ServicioDeAlmacenLocal _servicio;
    private readonly OpcionesDeAlmacenamiento _opciones;
    private readonly TimeProvider _reloj;
    private readonly ILogger<AlmacenLocalDeDocumentos> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AlmacenLocalDeDocumentos"/>.
    /// </summary>
    /// <param name="servicio">Núcleo del almacenamiento local.</param>
    /// <param name="opciones">Opciones de almacenamiento.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public AlmacenLocalDeDocumentos(
        ServicioDeAlmacenLocal servicio,
        IOptions<OpcionesDeAlmacenamiento> opciones,
        TimeProvider reloj,
        ILogger<AlmacenLocalDeDocumentos> logger)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _servicio = servicio;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<EnlaceTemporal> CrearEnlaceDeEscrituraAsync(string rutaBlob, CancellationToken cancellationToken = default)
    {
        DateTimeOffset expira = _reloj.GetUtcNow().AddMinutes(_opciones.MinutosVigenciaEscritura);
        Uri url = _servicio.CrearUrl(ServicioDeAlmacenLocal.PermisoEscritura, rutaBlob, expira, null);
        return Task.FromResult(new EnlaceTemporal(url, expira));
    }

    /// <inheritdoc/>
    public Task<EnlaceTemporal> CrearEnlaceDeLecturaAsync(
        string rutaBlob, string nombreDescarga, CancellationToken cancellationToken = default)
    {
        DateTimeOffset expira = _reloj.GetUtcNow().AddMinutes(_opciones.MinutosVigenciaLectura);
        Uri url = _servicio.CrearUrl(ServicioDeAlmacenLocal.PermisoLectura, rutaBlob, expira, nombreDescarga);
        return Task.FromResult(new EnlaceTemporal(url, expira));
    }

    /// <inheritdoc/>
    public Task<PropiedadesDeBlob?> ObtenerPropiedadesAsync(string rutaBlob, CancellationToken cancellationToken = default)
    {
        FileInfo? archivo = _servicio.ObtenerArchivo(rutaBlob);

        return Task.FromResult(archivo is null
            ? null
            : new PropiedadesDeBlob(archivo.Length, null, new DateTimeOffset(archivo.LastWriteTimeUtc, TimeSpan.Zero)));
    }

    /// <inheritdoc/>
    public Task<byte[]> LeerAsync(string rutaBlob, long bytesMaximos, CancellationToken cancellationToken = default)
        => _servicio.LeerAsync(rutaBlob, bytesMaximos, cancellationToken);

    /// <inheritdoc/>
    public Task MoverACuarentenaAsync(string rutaBlob, CancellationToken cancellationToken = default)
    {
        _servicio.MoverACuarentena(rutaBlob);
        _logger.LogWarning("Archivo {RutaBlob} trasladado a la carpeta de cuarentena local.", rutaBlob);
        return Task.CompletedTask;
    }
}
