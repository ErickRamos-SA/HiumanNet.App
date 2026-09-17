using HuimanNet.Contracts;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace HuimanNet.Hosting.Endpoints;

/// <summary>
/// Punto de entrada del almacenamiento local de documentos (sólo desarrollo),
/// común a la API y al portal web.
/// </summary>
/// <remarks>
/// Hace el papel del servicio Blob con SAS: la aplicación móvil y el navegador
/// suben (<c>PUT</c>) y descargan (<c>GET</c>) contra enlaces que el caso de uso
/// firma con permiso único y caducidad de minutos; aquí sólo se verifica la
/// firma. Por eso la ruta es anónima y no exige antifalsificación: la firma
/// <i>es</i> la autorización, igual que en Azure. Sólo se mapea con
/// <c>Almacenamiento:Proveedor = Local</c>; en Azure los clientes hablan
/// directamente con Blob Storage y esta ruta no existe.
/// </remarks>
public static class AlmacenLocalEndpoints
{
    /// <summary>
    /// Mapea la carga (<c>PUT</c>) y la descarga (<c>GET</c>) de archivos locales.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="app"/> es <c>null</c>.
    /// </exception>
    /// <remarks>
    /// Las rutas quedan fuera del documento OpenAPI: son un sustituto de
    /// desarrollo del almacenamiento, no parte del contrato.
    /// </remarks>
    public static IEndpointRouteBuilder MapearAlmacenLocal(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        const string plantilla = RutasApi.AlmacenLocal + "/{**ruta}";

        app.MapPut(plantilla, SubirAsync).AllowAnonymous().DisableAntiforgery().ExcludeFromDescription();
        app.MapGet(plantilla, Descargar).AllowAnonymous().ExcludeFromDescription();

        return app;
    }

    /// <summary>Recibe un archivo mediante una URL firmada del almacén local.</summary>
    /// <param name="ruta">Ruta del archivo dentro del almacén.</param>
    /// <param name="p">Permiso firmado.</param>
    /// <param name="e">Expiración firmada.</param>
    /// <param name="n">Nombre de descarga firmado.</param>
    /// <param name="s">Firma de la URL.</param>
    /// <param name="contexto">Contexto HTTP, para leer el cuerpo y limitar su tamaño.</param>
    /// <param name="servicio">Servicio del almacén local.</param>
    /// <param name="carga">Opciones de carga, para el tamaño máximo.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>201 si se guardó; 403 si la firma no es válida o expiró.</returns>
    private static async Task<IResult> SubirAsync(
        string ruta, string? p, string? e, string? n, string? s,
        HttpContext contexto,
        ServicioDeAlmacenLocal servicio,
        IOptions<OpcionesDeCarga> carga,
        CancellationToken cancellationToken)
    {
        if (!servicio.Validar(ruta, ServicioDeAlmacenLocal.PermisoEscritura, p, e, n, s))
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        long maximo = carga.Value.TamanoMaximoMegabytes * 1024L * 1024L;

        if (contexto.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limite)
        {
            limite.MaxRequestBodySize = maximo;
        }

        await servicio.GuardarAsync(ruta, contexto.Request.Body, maximo, cancellationToken);
        return TypedResults.Created();
    }

    /// <summary>Entrega un archivo mediante una URL firmada del almacén local.</summary>
    /// <param name="ruta">Ruta del archivo dentro del almacén.</param>
    /// <param name="p">Permiso firmado.</param>
    /// <param name="e">Expiración firmada.</param>
    /// <param name="n">Nombre de descarga firmado.</param>
    /// <param name="s">Firma de la URL.</param>
    /// <param name="servicio">Servicio del almacén local.</param>
    /// <returns>El archivo; 403 si la firma no es válida o expiró; 404 si no existe.</returns>
    private static IResult Descargar(string ruta, string? p, string? e, string? n, string? s, ServicioDeAlmacenLocal servicio)
    {
        if (!servicio.Validar(ruta, ServicioDeAlmacenLocal.PermisoLectura, p, e, n, s))
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        FileInfo? archivo = servicio.ObtenerArchivo(ruta);

        return archivo is null
            ? TypedResults.NotFound()
            : TypedResults.PhysicalFile(
                archivo.FullName, "application/octet-stream", string.IsNullOrEmpty(n) ? null : n, enableRangeProcessing: true);
    }
}
