using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Auditoria.Queries;
using HuimanNet.Application.Common;
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

    /// <summary>Consulta la bitácora de auditoría, paginada.</summary>
    /// <param name="empresaId">Empresa a filtrar.</param>
    /// <param name="desde">
    /// Inicio del intervalo; <see cref="VentanaDeBitacora.DiasPredeterminados"/>
    /// días antes del fin si se omite.
    /// </param>
    /// <param name="hasta">Fin del intervalo; el instante actual si se omite.</param>
    /// <param name="accion">Acción a filtrar.</param>
    /// <param name="usuarioId">Usuario a filtrar.</param>
    /// <param name="pagina">Página, desde 1; la primera si se omite.</param>
    /// <param name="tamanoPagina">Tamaño de página; <see cref="Paginacion.TamanoDeBitacora"/> si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="reloj">Reloj del sistema.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la página de asientos.</returns>
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
        DateTimeOffset limiteInferior = desde ?? limiteSuperior.AddDays(-VentanaDeBitacora.DiasPredeterminados);

        PaginaDto<RegistroAuditoriaDto> resultado = await manejador.EjecutarAsync(
            new ConsultarBitacoraQuery(
                empresaId,
                limiteInferior,
                limiteSuperior,
                accion,
                usuarioId,
                pagina ?? Paginacion.PaginaInicial,
                tamanoPagina ?? Paginacion.TamanoDeBitacora),
            cancellationToken);

        return TypedResults.Ok(resultado);
    }
}
