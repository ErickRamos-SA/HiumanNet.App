using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Periodos.Commands;
using HuimanNet.Application.Periodos.Queries;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Periodos;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints del recurso <c>periodos</c>.
/// </summary>
public static class PeriodosEndpoints
{
    /// <summary>
    /// Registra los endpoints de períodos de carga.
    /// </summary>
    /// <param name="app">Constructor de rutas de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="app"/> es <c>null</c>.
    /// </exception>
    public static IEndpointRouteBuilder MapearEndpointsDePeriodos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder grupo = app
            .MapGroup(RutasApi.Periodos)
            .WithTags("Periodos")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarPeriodos")
            .WithSummary("Lista los períodos de una empresa.")
            .Produces<IReadOnlyList<PeriodoDto>>();

        grupo.MapGet("/bandeja", ListarBandejaAsync)
            .WithName("ListarBandejaDelOperador")
            .WithSummary("Lista los períodos de todas las empresas que esperan acción del operador.")
            .RequireAuthorization(PoliticasDeAutorizacion.RolTransversal)
            .Produces<IReadOnlyList<PeriodoDto>>();

        grupo.MapPost("/", AbrirAsync)
            .WithName("AbrirPeriodo")
            .WithSummary("Abre un período de carga para una empresa cliente.")
            .RequireAuthorization(PoliticasDeAutorizacion.RolTransversal)
            .Produces<PeriodoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapPut("/{periodoId:guid}/estado", CambiarEstadoAsync)
            .WithName("CambiarEstadoDePeriodo")
            .WithSummary("Avanza un período al siguiente estado del ciclo.")
            .RequireAuthorization(PoliticasDeAutorizacion.RolTransversal)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    /// <summary>Lista los períodos de una empresa.</summary>
    /// <param name="empresaId">Empresa consultada; la del usuario si se omite.</param>
    /// <param name="incluirCerrados">Incluye los cerrados; verdadero si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los períodos.</returns>
    private static async Task<IResult> ListarAsync(
        Guid? empresaId,
        bool? incluirCerrados,
        IManejadorDeConsulta<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>> manejador,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PeriodoDto> periodos = await manejador.EjecutarAsync(
            new ListarPeriodosQuery(empresaId, incluirCerrados ?? true), cancellationToken);

        return TypedResults.Ok(periodos);
    }

    /// <summary>Lista los períodos pendientes de todas las empresas para la bandeja del operador.</summary>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los períodos.</returns>
    private static async Task<IResult> ListarBandejaAsync(
        IManejadorDeConsulta<ListarBandejaOperadorQuery, IReadOnlyList<PeriodoDto>> manejador,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PeriodoDto> periodos =
            await manejador.EjecutarAsync(new ListarBandejaOperadorQuery(), cancellationToken);

        return TypedResults.Ok(periodos);
    }

    /// <summary>Abre un período de carga.</summary>
    /// <param name="peticion">Empresa, año, mes, consecutivo, descripción y fecha límite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>201 con el período y su ubicación.</returns>
    private static async Task<IResult> AbrirAsync(
        AbrirPeriodoRequest peticion,
        IManejadorDeComando<AbrirPeriodoCommand, PeriodoDto> manejador,
        CancellationToken cancellationToken)
    {
        var comando = new AbrirPeriodoCommand(
            peticion.EmpresaId,
            peticion.Anio,
            peticion.Mes,
            peticion.Consecutivo,
            peticion.Descripcion,
            peticion.FechaLimiteCarga);

        PeriodoDto periodo = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Created($"{RutasApi.Periodos}/{periodo.Id}", periodo);
    }

    /// <summary>Avanza el estado de un período.</summary>
    /// <param name="periodoId">Período afectado.</param>
    /// <param name="empresaId">Empresa del período; la del usuario si se omite.</param>
    /// <param name="peticion">Estado nuevo y comentario.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se cambió.</returns>
    private static async Task<IResult> CambiarEstadoAsync(
        Guid periodoId,
        Guid? empresaId,
        CambiarEstadoPeriodoRequest peticion,
        IManejadorDeComando<CambiarEstadoPeriodoCommand> manejador,
        CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(
            new CambiarEstadoPeriodoCommand(periodoId, peticion.NuevoEstado, peticion.Comentario, empresaId),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
