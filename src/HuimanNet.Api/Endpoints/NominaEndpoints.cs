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

    private static async Task<Ok<IReadOnlyList<CorridaDeNominaDto>>> ListarCorridasAsync(
        Guid periodoId,
        Guid? empresaId,
        IManejadorDeConsulta<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarCorridasQuery(periodoId, empresaId), cancellationToken));

    private static async Task<Ok<CorridaDeNominaDto>> CalcularAsync(
        CalcularNominaRequest peticion,
        IManejadorDeComando<CalcularNominaCommand, CorridaDeNominaDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new CalcularNominaCommand(peticion.PeriodoId, peticion.EmpresaId, peticion.Observaciones), cancellationToken));

    private static async Task<Ok<ResumenDeCorridaDto>> ObtenerResumenAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerResumenDeCorridaQuery, ResumenDeCorridaDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerResumenDeCorridaQuery(corridaId, empresaId), cancellationToken));

    private static async Task<Ok<DetalleDeResultadoDto>> ObtenerDetalleAsync(
        Guid resultadoId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerDetalleDeResultadoQuery, DetalleDeResultadoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerDetalleDeResultadoQuery(resultadoId, empresaId), cancellationToken));

    private static async Task<FileContentHttpResult> ExportarAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ExportarCorridaQuery, ArchivoExportado> manejador,
        CancellationToken cancellationToken)
    {
        ArchivoExportado archivo = await manejador.EjecutarAsync(new ExportarCorridaQuery(corridaId, empresaId), cancellationToken);
        return TypedResults.File(archivo.Contenido, archivo.TipoDeContenido, archivo.Nombre);
    }

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

    private static async Task<Ok<CotejoDto>> CotejarAsync(
        CotejarNominaRequest peticion,
        IManejadorDeComando<CotejarNominaCommand, CotejoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new CotejarNominaCommand(peticion.CorridaId, peticion.EmpresaId, peticion.DocumentoId, peticion.ToleranciaAbsoluta),
            cancellationToken));

    private static async Task<Ok<IReadOnlyList<CotejoDto>>> ListarCotejosAsync(
        Guid corridaId,
        Guid? empresaId,
        IManejadorDeConsulta<ListarCotejosQuery, IReadOnlyList<CotejoDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarCotejosQuery(corridaId, empresaId), cancellationToken));

    private static async Task<Ok<CotejoDto>> ObtenerCotejoAsync(
        Guid cotejoId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerCotejoQuery, CotejoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerCotejoQuery(cotejoId, empresaId), cancellationToken));
}
