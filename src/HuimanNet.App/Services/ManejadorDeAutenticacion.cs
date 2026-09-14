using System.Net;
using System.Net.Http.Headers;
using HuimanNet.Contracts;

namespace HuimanNet.App.Services;

/// <summary>
/// Añade el token de acceso a cada llamada a la API.
/// </summary>
/// <remarks>
/// Al ser un <see cref="DelegatingHandler"/>, ninguna ViewModel ni servicio
/// necesita manipular encabezados de autorización: la política de autenticación
/// vive en un solo sitio.
/// </remarks>
public sealed class ManejadorDeAutenticacion : DelegatingHandler
{
    private readonly IProveedorDeToken _proveedor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ManejadorDeAutenticacion"/>.
    /// </summary>
    /// <param name="proveedor">Proveedor de tokens de acceso.</param>
    public ManejadorDeAutenticacion(IProveedorDeToken proveedor) => _proveedor = proveedor;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Rutas anónimas (configuración pública e inicio de sesión local): no se
        // pide token. Además de ahorrar la llamada, evita que el primer contacto
        // con la API espere al proveedor de identidad cuando aún no se conoce el
        // modo que usa el servidor.
        if (EsRutaAnonima(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        string? token = await _proveedor.ObtenerTokenSilenciosoAsync(cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage respuesta = await base.SendAsync(request, cancellationToken);

        // Un 401 tras un token silencioso significa que la caché quedó obsoleta:
        // se pide uno nuevo y se reintenta una única vez.
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            respuesta.Dispose();

            string renovado = await _proveedor.IniciarSesionAsync(cancellationToken);

            using HttpRequestMessage reintento = await ClonarAsync(request, cancellationToken);
            reintento.Headers.Authorization = new AuthenticationHeaderValue("Bearer", renovado);

            return await base.SendAsync(reintento, cancellationToken);
        }

        return respuesta;
    }

    /// <summary>
    /// Indica si una petición va a un endpoint anónimo de la API.
    /// </summary>
    /// <param name="uri">Dirección de la petición.</param>
    /// <returns><c>true</c> para la configuración pública, el inicio de sesión local y la salud.</returns>
    private static bool EsRutaAnonima(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        string ruta = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;

        return ruta.StartsWith(RutasApi.Configuracion, StringComparison.OrdinalIgnoreCase)
            || ruta.StartsWith(RutasApi.IniciarSesion, StringComparison.OrdinalIgnoreCase)
            || ruta.StartsWith(RutasApi.Salud, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Duplica una petición para poder reenviarla.
    /// </summary>
    /// <param name="original">Petición ya enviada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una copia lista para enviarse de nuevo.</returns>
    /// <remarks>
    /// Un <see cref="HttpRequestMessage"/> no puede reenviarse: su contenido ya
    /// se consumió. Por eso el reintento se hace sobre una copia con el cuerpo
    /// materializado en memoria.
    /// </remarks>
    private static async Task<HttpRequestMessage> ClonarAsync(
        HttpRequestMessage original, CancellationToken cancellationToken)
    {
        var copia = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy,
        };

        if (original.Content is not null)
        {
            byte[] cuerpo = await original.Content.ReadAsByteArrayAsync(cancellationToken);
            var contenido = new ByteArrayContent(cuerpo);

            foreach (var encabezado in original.Content.Headers)
            {
                contenido.Headers.TryAddWithoutValidation(encabezado.Key, encabezado.Value);
            }

            copia.Content = contenido;
        }

        foreach (var encabezado in original.Headers)
        {
            copia.Headers.TryAddWithoutValidation(encabezado.Key, encabezado.Value);
        }

        return copia;
    }
}
