using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Empleados;
using HuimanNet.Application.Incidencias;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Incidencias;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints de empleados, contratos e incidencias.
/// </summary>
public static class EmpleadosEndpoints
{
    /// <summary>Tamaño de página por omisión del listado de empleados.</summary>
    public const int TamanoDePaginaPredeterminado = 50;

    /// <summary>
    /// Mapea los endpoints de empleados, contratos e incidencias.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeEmpleados(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder empleados = app.MapGroup(RutasApi.Empleados)
            .WithTags("Empleados")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        empleados.MapGet("/", ListarAsync)
            .WithName("ListarEmpleados")
            .WithSummary("Lista paginada de empleados. Los roles transversales pueden omitir la empresa para ver todas.");
        empleados.MapGet("/{empleadoId:guid}", ObtenerAsync).WithName("ObtenerEmpleado").ProducesProblem(StatusCodes.Status404NotFound);
        empleados.MapPost("/", CrearAsync).WithName("CrearEmpleado").ProducesProblem(StatusCodes.Status400BadRequest);
        empleados.MapPut("/{empleadoId:guid}", ActualizarAsync).WithName("ActualizarEmpleado").ProducesProblem(StatusCodes.Status400BadRequest);
        empleados.MapPost("/{empleadoId:guid}/contratos", CrearContratoAsync).WithName("CrearContrato").ProducesProblem(StatusCodes.Status400BadRequest);
        empleados.MapPut("/{empleadoId:guid}/contratos/{contratoId:guid}", ActualizarContratoAsync).WithName("ActualizarContrato").ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet(RutasApi.Periodos + "/{periodoId:guid}/incidencias", ListarIncidenciasAsync)
            .WithTags("Incidencias")
            .WithName("ListarIncidenciasDePeriodo")
            .WithSummary("Una fila por contrato vigente, con la incidencia capturada o las cantidades de un período completo.")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        RouteGroupBuilder incidencias = app.MapGroup(RutasApi.Incidencias)
            .WithTags("Incidencias")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        incidencias.MapPut("/", GuardarIncidenciaAsync).WithName("GuardarIncidencia").ProducesProblem(StatusCodes.Status400BadRequest);
        incidencias.MapDelete("/{incidenciaId:guid}", EliminarIncidenciaAsync).WithName("EliminarIncidencia");
        incidencias.MapPost("/importar", ImportarIncidenciasAsync)
            .WithName("ImportarIncidencias")
            .WithSummary("Importa las incidencias de un archivo de incidencias (CSV o XLSX) ya cargado en los documentos del período.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Ok<PaginaDto<EmpleadoResumenDto>>> ListarAsync(
        Guid? empresaId,
        bool? soloActivos,
        string? texto,
        int? pagina,
        int? tamanoPagina,
        IManejadorDeConsulta<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new ListarEmpleadosQuery(empresaId, soloActivos ?? true, texto, pagina ?? 1, tamanoPagina ?? TamanoDePaginaPredeterminado),
            cancellationToken));

    private static async Task<Ok<EmpleadoDto>> ObtenerAsync(
        Guid empleadoId,
        Guid? empresaId,
        IManejadorDeConsulta<ObtenerEmpleadoQuery, EmpleadoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerEmpleadoQuery(empleadoId, empresaId), cancellationToken));

    private static async Task<Created<EmpleadoDto>> CrearAsync(
        GuardarEmpleadoRequest peticion,
        IManejadorDeComando<GuardarEmpleadoCommand, EmpleadoDto> manejador,
        CancellationToken cancellationToken)
    {
        EmpleadoDto empleado = await manejador.EjecutarAsync(new GuardarEmpleadoCommand(null, peticion), cancellationToken);
        return TypedResults.Created(RutasApi.Recurso(RutasApi.Empleados, empleado.Id), empleado);
    }

    private static async Task<Ok<EmpleadoDto>> ActualizarAsync(
        Guid empleadoId,
        GuardarEmpleadoRequest peticion,
        IManejadorDeComando<GuardarEmpleadoCommand, EmpleadoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarEmpleadoCommand(empleadoId, peticion), cancellationToken));

    private static async Task<Created<ContratoDto>> CrearContratoAsync(
        Guid empleadoId,
        GuardarContratoRequest peticion,
        IManejadorDeComando<GuardarContratoCommand, ContratoDto> manejador,
        CancellationToken cancellationToken)
    {
        ContratoDto contrato = await manejador.EjecutarAsync(new GuardarContratoCommand(empleadoId, null, peticion), cancellationToken);
        return TypedResults.Created(RutasApi.ContratosDeEmpleado(empleadoId), contrato);
    }

    private static async Task<Ok<ContratoDto>> ActualizarContratoAsync(
        Guid empleadoId,
        Guid contratoId,
        GuardarContratoRequest peticion,
        IManejadorDeComando<GuardarContratoCommand, ContratoDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarContratoCommand(empleadoId, contratoId, peticion), cancellationToken));

    private static async Task<Ok<IReadOnlyList<IncidenciaDto>>> ListarIncidenciasAsync(
        Guid periodoId,
        Guid? empresaId,
        IManejadorDeConsulta<ListarIncidenciasQuery, IReadOnlyList<IncidenciaDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarIncidenciasQuery(periodoId, empresaId), cancellationToken));

    private static async Task<Ok<IncidenciaDto>> GuardarIncidenciaAsync(
        GuardarIncidenciaRequest peticion,
        IManejadorDeComando<GuardarIncidenciaCommand, IncidenciaDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarIncidenciaCommand(peticion), cancellationToken));

    private static async Task<NoContent> EliminarIncidenciaAsync(
        Guid incidenciaId,
        Guid? empresaId,
        IManejadorDeComando<EliminarIncidenciaCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new EliminarIncidenciaCommand(incidenciaId, empresaId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ResultadoDeImportacionDto>> ImportarIncidenciasAsync(
        ImportarIncidenciasRequest peticion,
        IManejadorDeComando<ImportarIncidenciasCommand, ResultadoDeImportacionDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new ImportarIncidenciasCommand(peticion.DocumentoId, peticion.EmpresaId), cancellationToken));
}
