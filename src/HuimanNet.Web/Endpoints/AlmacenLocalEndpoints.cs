using HuimanNet.Contracts;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace HuimanNet.Web.Endpoints;

/// <summary>
/// Punto de entrada del almacenamiento local de documentos para el portal web
/// (sólo desarrollo).
/// </summary>
/// <remarks>
/// Hace el papel del servicio Blob: el navegador sube (<c>PUT</c>) y descarga
/// (<c>GET</c>) contra enlaces firmados emitidos por los casos de uso. La firma
/// es la autorización, por eso la ruta es anónima y no exige antifalsificación.
/// Sólo se mapea con <c>Almacenamiento:Proveedor = Local</c>; en Azure el
/// navegador habla directamente con Blob Storage y esta ruta no existe.
/// </remarks>
public static class AlmacenLocalEndpoints
{
    /// <summary>
    /// Mapea la carga y la descarga de archivos locales.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearAlmacenLocal(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(RutasApi.AlmacenLocal + "/{**ruta}", SubirAsync).AllowAnonymous().DisableAntiforgery();
        app.MapGet(RutasApi.AlmacenLocal + "/{**ruta}", Descargar).AllowAnonymous();

        return app;
    }

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
