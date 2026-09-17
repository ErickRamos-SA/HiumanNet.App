using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos.Queries;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints anidados en el recurso <c>periodos</c> que devuelven documentos.
/// </summary>
public static class DocumentosDePeriodoEndpoints
{
    /// <summary>
    /// Registra el listado de documentos de un período.
    /// </summary>
    /// <param name="app">Constructor de rutas de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="app"/> es <c>null</c>.
    /// </exception>
    public static IEndpointRouteBuilder MapearDocumentosDePeriodo(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(RutasApi.Periodos + "/{periodoId:guid}/documentos", ListarAsync)
            .WithTags("Documentos")
            .WithName("ListarDocumentosDePeriodo")
            .WithSummary("Lista los documentos de un período.")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal)
            .Produces<IReadOnlyList<DocumentoDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>Lista los documentos de un período.</summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="tipo">Tipo a filtrar; todos si se omite.</param>
    /// <param name="soloDescargables">Sólo los que superaron el escaneo; falso si se omite.</param>
    /// <param name="empresaId">Empresa del período; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los documentos.</returns>
    private static async Task<IResult> ListarAsync(
        Guid periodoId,
        TipoDocumento? tipo,
        bool? soloDescargables,
        Guid? empresaId,
        IManejadorDeConsulta<ListarDocumentosPorPeriodoQuery, IReadOnlyList<DocumentoDto>> manejador,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DocumentoDto> documentos = await manejador.EjecutarAsync(
            new ListarDocumentosPorPeriodoQuery(periodoId, tipo, soloDescargables ?? false, empresaId),
            cancellationToken);

        return TypedResults.Ok(documentos);
    }
}
