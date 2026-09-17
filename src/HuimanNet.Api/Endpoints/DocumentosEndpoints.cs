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

    /// <summary>Reserva un documento y devuelve la URL firmada para subirlo.</summary>
    /// <param name="peticion">Período, tipo, nombre y tamaño del archivo.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>201 con la URL de carga y la ruta para confirmarla.</returns>
    private static async Task<IResult> SolicitarCargaAsync(
        SolicitarCargaRequest peticion,
        IManejadorDeComando<SolicitarCargaDocumentoCommand, SolicitarCargaResponse> manejador,
        CancellationToken cancellationToken)
    {
        var comando = new SolicitarCargaDocumentoCommand(
            peticion.PeriodoId,
            peticion.Tipo,
            peticion.NombreArchivo,
            peticion.TamanoBytes,
            peticion.EmpresaId);

        SolicitarCargaResponse respuesta = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Created(RutasApi.ConfirmarCarga(respuesta.DocumentoId), respuesta);
    }

    /// <summary>Confirma que el archivo se subió y registra su huella.</summary>
    /// <param name="documentoId">Documento reservado.</param>
    /// <param name="peticion">Huella SHA-256 calculada por el cliente.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el documento.</returns>
    private static async Task<IResult> ConfirmarCargaAsync(
        Guid documentoId,
        ConfirmarCargaRequest peticion,
        IManejadorDeComando<ConfirmarCargaCommand, DocumentoDto> manejador,
        CancellationToken cancellationToken)
    {
        var comando = new ConfirmarCargaCommand(documentoId, peticion.HuellaSha256);

        DocumentoDto documento = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Ok(documento);
    }

    /// <summary>Emite una URL firmada de corta duración para descargar un documento.</summary>
    /// <param name="documentoId">Documento solicitado.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el enlace.</returns>
    private static async Task<IResult> ObtenerEnlaceDescargaAsync(
        Guid documentoId,
        IManejadorDeConsulta<ObtenerEnlaceDescargaQuery, EnlaceDescargaResponse> manejador,
        CancellationToken cancellationToken)
    {
        EnlaceDescargaResponse enlace = await manejador.EjecutarAsync(
            new ObtenerEnlaceDescargaQuery(documentoId), cancellationToken);

        return TypedResults.Ok(enlace);
    }

    /// <summary>Registra el veredicto del escaneo de malware de un blob.</summary>
    /// <param name="peticion">Ruta del blob, veredicto y motivo.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>202 si se registró.</returns>
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
