using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Nomina;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints del cálculo de nómina, sus resultados, exportación y cotejo contra
/// el cálculo manual.
/// </summary>
public static class NominaEndpoints
{
    /// <summary>
    /// Mapea los endpoints de nómina.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeNomina(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(RutasApi.Periodos + "/{periodoId:guid}/nomina", ListarCorridasAsync)
            .WithTags("Nómina")
            .WithName("ListarCorridasDePeriodo")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        RouteGroupBuilder nomina = app.MapGroup(RutasApi.Nomina)
            .WithTags("Nómina")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        nomina.MapPost("/calcular", CalcularAsync)
            .WithName("CalcularNomina")
            .WithSummary("Calcula la nómina de un período con el catálogo vigente y guarda una corrida nueva.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        nomina.MapGet("/{corridaId:guid}/resumen", ObtenerResumenAsync).WithName("ObtenerResumenDeCorrida");
        nomina.MapGet("/resultados/{resultadoId:guid}", ObtenerDetalleAsync).WithName("ObtenerDetalleDeResultado");
        nomina.MapGet("/{corridaId:guid}/exportar", ExportarAsync)
            .WithName("ExportarCorrida")
            .WithSummary("Descarga la corrida como archivo CSV (una fila por trabajador y una columna por concepto).");
        nomina.MapPut("/{corridaId:guid}/estado", CambiarEstadoAsync).WithName("CambiarEstadoDeCorrida").ProducesProblem(StatusCodes.Status409Conflict);

        nomina.MapPost("/cotejar", CotejarAsync)
            .WithName("CotejarNomina")
            .WithSummary("Compara la corrida contra el archivo del cálculo manual publicado en los documentos del mismo período.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        nomina.MapGet("/{corridaId:guid}/cotejos", ListarCotejosAsync).WithName("ListarCotejos");
        nomina.MapGet("/cotejos/{cotejoId:guid}", ObtenerCotejoAsync).WithName("ObtenerCotejo");

        return app;
    }

    /// <summary>Lista las corridas de un período.</summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del período; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con las corridas.</returns>
    private static async Task<Ok<IReadOnlyList<CorridaDeNominaDto>>> ListarCorridasAsync(
        Guid periodoId,
        Guid? empresaId,
        IManejadorDeConsulta<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarCorridasQuery(periodoId, empresaId), cancellationToken));

    /// <summary>Calcula la nómina de un período en una corrida nueva.</summary>
    /// <param name="peticion">Período, empresa y observaciones.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la corrida calculada.</returns>
    private static async Task<Ok<CorridaDeNominaDto>> CalcularAsync(
        CalcularNominaRequest peticion,
        IManejadorDeComando<CalcularNominaCommand, CorridaDeNominaDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new CalcularNominaCommand(peticion.PeriodoId, peticion.EmpresaId, peticion.Observaciones), cancellationToken));

    /// <summary>Obtiene el resumen de una corrida con sus resultados por contrato.</summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa de la corrida; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el resumen.</returns>
    private static async Task<Ok<ResumenDeCorridaDto>> ObtenerResumenAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerResumenDeCorridaQuery, ResumenDeCorridaDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerResumenDeCorridaQuery(corridaId, empresaId), cancellationToken));

    /// <summary>Obtiene el detalle de conceptos del resultado de un contrato.</summary>
    /// <param name="resultadoId">Resultado consultado.</param>
    /// <param name="empresaId">Empresa del resultado; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el detalle.</returns>
    private static async Task<Ok<DetalleDeResultadoDto>> ObtenerDetalleAsync(
        Guid resultadoId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerDetalleDeResultadoQuery, DetalleDeResultadoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerDetalleDeResultadoQuery(resultadoId, empresaId), cancellationToken));

    /// <summary>Exporta los resultados de una corrida en CSV.</summary>
    /// <param name="corridaId">Corrida a exportar.</param>
    /// <param name="empresaId">Empresa de la corrida; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>El archivo CSV.</returns>
    private static async Task<FileContentHttpResult> ExportarAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ExportarCorridaQuery, ArchivoExportado> manejador,
        CancellationToken cancellationToken)
    {
        ArchivoExportado archivo = await manejador.EjecutarAsync(new ExportarCorridaQuery(corridaId, empresaId), cancellationToken);
        return TypedResults.File(archivo.Contenido, archivo.TipoDeContenido, archivo.Nombre);
    }

    /// <summary>Cambia el estado de una corrida.</summary>
    /// <param name="corridaId">Corrida afectada.</param>
    /// <param name="peticion">Estado nuevo, empresa y observaciones.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se cambió.</returns>
    private static async Task<NoContent> CambiarEstadoAsync(
        Guid corridaId,
        CambiarEstadoCorridaRequest peticion,
        IManejadorDeComando<CambiarEstadoCorridaCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(
            new CambiarEstadoCorridaCommand(corridaId, peticion.Estado, peticion.EmpresaId, peticion.Observaciones), cancellationToken);

        return TypedResults.NoContent();
    }

    /// <summary>Compara una corrida con el resultado manual cargado como documento.</summary>
    /// <param name="peticion">Corrida, empresa, documento y tolerancia.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el cotejo y sus diferencias.</returns>
    private static async Task<Ok<CotejoDto>> CotejarAsync(
        CotejarNominaRequest peticion,
        IManejadorDeComando<CotejarNominaCommand, CotejoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new CotejarNominaCommand(peticion.CorridaId, peticion.EmpresaId, peticion.DocumentoId, peticion.ToleranciaAbsoluta),
            cancellationToken));

    /// <summary>Lista los cotejos de una corrida.</summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa de la corrida; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los cotejos.</returns>
    private static async Task<Ok<IReadOnlyList<CotejoDto>>> ListarCotejosAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ListarCotejosQuery, IReadOnlyList<CotejoDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarCotejosQuery(corridaId, empresaId), cancellationToken));

    /// <summary>Obtiene un cotejo con sus diferencias.</summary>
    /// <param name="cotejoId">Cotejo consultado.</param>
    /// <param name="empresaId">Empresa del cotejo; la del usuario si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el cotejo.</returns>
    private static async Task<Ok<CotejoDto>> ObtenerCotejoAsync(
        Guid cotejoId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerCotejoQuery, CotejoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerCotejoQuery(cotejoId, empresaId), cancellationToken));
}
