using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Application.Documentos.Queries;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints del recurso <c>documentos</c>.
/// </summary>
/// <remarks>
/// Los endpoints son deliberadamente delgados: traducen HTTP a un comando o
/// consulta y devuelven el resultado. Ninguna regla de negocio vive aquí, para
/// que la web Blazor obtenga exactamente el mismo comportamiento invocando los
/// casos de uso en proceso.
/// </remarks>
public static class DocumentosEndpoints
{
    /// <summary>
    /// Registra los endpoints de documentos.
    /// </summary>
    /// <param name="app">Constructor de rutas de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="app"/> es <c>null</c>.
    /// </exception>
    public static IEndpointRouteBuilder MapearEndpointsDeDocumentos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder grupo = app
            .MapGroup(RutasApi.Documentos)
            .WithTags("Documentos")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        grupo.MapPost("/solicitar-carga", SolicitarCargaAsync)
            .WithName("SolicitarCargaDeDocumento")
            .WithSummary("Autoriza una carga y devuelve una URL SAS de escritura.")
            .Produces<SolicitarCargaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/{documentoId:guid}/confirmar", ConfirmarCargaAsync)
            .WithName("ConfirmarCargaDeDocumento")
            .WithSummary("Confirma que el archivo terminó de subirse.")
            .Produces<DocumentoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapGet("/{documentoId:guid}/enlace-descarga", ObtenerEnlaceDescargaAsync)
            .WithName("ObtenerEnlaceDeDescarga")
            .WithSummary("Devuelve una URL SAS de lectura y audita la descarga.")
            .Produces<EnlaceDescargaResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Punto de entrada de Defender for Storage: identidad de servicio, no de usuario.
        app.MapPost(RutasApi.ResultadoEscaneo, RegistrarResultadoEscaneoAsync)
            .WithTags("Documentos")
            .WithName("RegistrarResultadoDeEscaneo")
            .WithSummary("Registra el veredicto del escaneo de malware de un blob.")
            .RequireAuthorization(PoliticasDeAutorizacion.ServicioDeEscaneo)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> SolicitarCargaAsync(
        SolicitarCargaRequest peticion,
        IValidadorDeEntrada<SolicitarCargaDocumentoCommand> validador,
        IManejadorDeComando<SolicitarCargaDocumentoCommand, SolicitarCargaResponse> manejador,
        CancellationToken cancellationToken)
    {
        var comando = new SolicitarCargaDocumentoCommand(
            peticion.PeriodoId,
            peticion.Tipo,
            peticion.NombreArchivo,
            peticion.TamanoBytes,
            peticion.EmpresaId);

        validador.Validar(comando).GarantizarValido();

        SolicitarCargaResponse respuesta = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Created(RutasApi.ConfirmarCarga(respuesta.DocumentoId), respuesta);
    }

    private static async Task<IResult> ConfirmarCargaAsync(
        Guid documentoId,
        ConfirmarCargaRequest peticion,
        IValidadorDeEntrada<ConfirmarCargaCommand> validador,
        IManejadorDeComando<ConfirmarCargaCommand, DocumentoDto> manejador,
        CancellationToken cancellationToken)
    {
        var comando = new ConfirmarCargaCommand(documentoId, peticion.HuellaSha256);

        validador.Validar(comando).GarantizarValido();

        DocumentoDto documento = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Ok(documento);
    }

    private static async Task<IResult> ObtenerEnlaceDescargaAsync(
        Guid documentoId,
        IManejadorDeConsulta<ObtenerEnlaceDescargaQuery, EnlaceDescargaResponse> manejador,
        CancellationToken cancellationToken)
    {
        EnlaceDescargaResponse enlace = await manejador.EjecutarAsync(
            new ObtenerEnlaceDescargaQuery(documentoId), cancellationToken);

        return TypedResults.Ok(enlace);
    }

    private static async Task<IResult> RegistrarResultadoEscaneoAsync(
        RegistrarResultadoEscaneoRequest peticion,
        IManejadorDeComando<RegistrarResultadoEscaneoCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(
            new RegistrarResultadoEscaneoCommand(peticion.RutaBlob, peticion.EsLimpio, peticion.Motivo),
            cancellationToken);

        return TypedResults.Accepted((string?)null);
    }
}

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
