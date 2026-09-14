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

    private static async Task<IResult> ListarBandejaAsync(
        IManejadorDeConsulta<ListarBandejaOperadorQuery, IReadOnlyList<PeriodoDto>> manejador,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PeriodoDto> periodos =
            await manejador.EjecutarAsync(new ListarBandejaOperadorQuery(), cancellationToken);

        return TypedResults.Ok(periodos);
    }

    private static async Task<IResult> AbrirAsync(
        AbrirPeriodoRequest peticion,
        IValidadorDeEntrada<AbrirPeriodoCommand> validador,
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

        validador.Validar(comando).GarantizarValido();

        PeriodoDto periodo = await manejador.EjecutarAsync(comando, cancellationToken);

        return TypedResults.Created($"{RutasApi.Periodos}/{periodo.Id}", periodo);
    }

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
