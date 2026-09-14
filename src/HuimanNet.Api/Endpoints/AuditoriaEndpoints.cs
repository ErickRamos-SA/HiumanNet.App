using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Auditoria.Queries;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Common;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints del recurso <c>auditoria</c>.
/// </summary>
public static class AuditoriaEndpoints
{
    /// <summary>Ventana temporal que se consulta cuando el cliente no indica fechas.</summary>
    private static readonly TimeSpan VentanaPredeterminada = TimeSpan.FromDays(30);

    /// <summary>
    /// Registra los endpoints de la bitácora de auditoría.
    /// </summary>
    /// <param name="app">Constructor de rutas de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="app"/> es <c>null</c>.
    /// </exception>
    public static IEndpointRouteBuilder MapearEndpointsDeAuditoria(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(RutasApi.Auditoria, ConsultarAsync)
            .WithTags("Auditoría")
            .WithName("ConsultarBitacora")
            .WithSummary("Consulta la bitácora de cargas y descargas.")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal)
            .Produces<PaginaDto<RegistroAuditoriaDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ConsultarAsync(
        Guid? empresaId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        AccionAuditada? accion,
        Guid? usuarioId,
        int? pagina,
        int? tamanoPagina,
        IManejadorDeConsulta<ConsultarBitacoraQuery, PaginaDto<RegistroAuditoriaDto>> manejador,
        TimeProvider reloj,
        CancellationToken cancellationToken)
    {
        DateTimeOffset limiteSuperior = hasta ?? reloj.GetUtcNow();
        DateTimeOffset limiteInferior = desde ?? limiteSuperior - VentanaPredeterminada;

        PaginaDto<RegistroAuditoriaDto> resultado = await manejador.EjecutarAsync(
            new ConsultarBitacoraQuery(
                empresaId,
                limiteInferior,
                limiteSuperior,
                accion,
                usuarioId,
                pagina ?? 1,
                tamanoPagina ?? IConsultasAuditoria.TamanoPaginaPredeterminado),
            cancellationToken);

        return TypedResults.Ok(resultado);
    }
}
