using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Common;
using HuimanNet.Application.Empresas;
using HuimanNet.Application.Empresas.Queries;
using HuimanNet.Application.RazonesSociales;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Empresas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints del catálogo de empresas cliente y de sus razones sociales.
/// </summary>
public static class EmpresasEndpoints
{
    /// <summary>
    /// Mapea los endpoints de empresas y razones sociales.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeEmpresas(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder empresas = app.MapGroup(RutasApi.Empresas)
            .WithTags("Empresas")
            .RequireAuthorization(PoliticasDeAutorizacion.RolTransversal);

        empresas.MapGet("/", ListarAsync)
            .WithName("ListarEmpresas")
            .WithSummary("Lista el catálogo de empresas cliente.");

        empresas.MapGet("/detalle", ListarDetalleAsync)
            .WithName("ListarEmpresasConDetalle")
            .WithSummary("Lista las empresas con sus contadores administrativos.");

        empresas.MapGet("/{empresaId:guid}", ObtenerAsync)
            .WithName("ObtenerEmpresa")
            .ProducesProblem(StatusCodes.Status404NotFound);

        empresas.MapPost("/", CrearAsync)
            .WithName("CrearEmpresa")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        empresas.MapPut("/{empresaId:guid}", ActualizarAsync)
            .WithName("ActualizarEmpresa")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet(RutasApi.Empresas + "/actual", ObtenerActualAsync)
            .WithTags("Empresas")
            .WithName("ObtenerEmpresaDelUsuario")
            .WithSummary("Detalle de la empresa del usuario de empresa cliente.")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        RouteGroupBuilder razones = app.MapGroup(RutasApi.RazonesSociales)
            .WithTags("Razones sociales")
            .RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

        razones.MapGet("/", ListarRazonesSocialesAsync).WithName("ListarRazonesSociales");
        razones.MapPost("/", CrearRazonSocialAsync).WithName("CrearRazonSocial").ProducesProblem(StatusCodes.Status400BadRequest);
        razones.MapPut("/{razonSocialId:guid}", ActualizarRazonSocialAsync).WithName("ActualizarRazonSocial").ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<EmpresaDto>>> ListarAsync(
        bool? soloActivas,
        IManejadorDeConsulta<ListarEmpresasQuery, IReadOnlyList<EmpresaDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarEmpresasQuery(soloActivas ?? true), cancellationToken));

    private static async Task<Ok<IReadOnlyList<EmpresaDetalleDto>>> ListarDetalleAsync(
        bool? soloActivas,
        IManejadorDeConsulta<ListarEmpresasDetalleQuery, IReadOnlyList<EmpresaDetalleDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarEmpresasDetalleQuery(soloActivas ?? false), cancellationToken));

    private static async Task<Ok<EmpresaDetalleDto>> ObtenerAsync(
        Guid empresaId,
        IManejadorDeConsulta<ObtenerEmpresaQuery, EmpresaDetalleDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerEmpresaQuery(empresaId), cancellationToken));

    private static async Task<Ok<EmpresaDetalleDto>> ObtenerActualAsync(
        IManejadorDeConsulta<ObtenerEmpresaQuery, EmpresaDetalleDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerEmpresaQuery(null), cancellationToken));

    private static async Task<Created<EmpresaDetalleDto>> CrearAsync(
        GuardarEmpresaRequest peticion,
        IManejadorDeComando<CrearEmpresaCommand, EmpresaDetalleDto> manejador,
        CancellationToken cancellationToken)
    {
        EmpresaDetalleDto empresa = await manejador.EjecutarAsync(
            new CrearEmpresaCommand(peticion.RazonSocial, peticion.IdentificadorFiscal), cancellationToken);

        return TypedResults.Created(RutasApi.Recurso(RutasApi.Empresas, empresa.Id), empresa);
    }

    private static async Task<Ok<EmpresaDetalleDto>> ActualizarAsync(
        Guid empresaId,
        GuardarEmpresaRequest peticion,
        IManejadorDeComando<ActualizarEmpresaCommand, EmpresaDetalleDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new ActualizarEmpresaCommand(empresaId, peticion.RazonSocial, peticion.IdentificadorFiscal, peticion.Activa),
            cancellationToken));

    private static async Task<Ok<IReadOnlyList<RazonSocialDto>>> ListarRazonesSocialesAsync(
        Guid? empresaId,
        bool? soloActivas,
        IManejadorDeConsulta<ListarRazonesSocialesQuery, IReadOnlyList<RazonSocialDto>> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new ListarRazonesSocialesQuery(empresaId, soloActivas ?? false), cancellationToken));

    private static async Task<Created<RazonSocialDto>> CrearRazonSocialAsync(
        GuardarRazonSocialRequest peticion,
        IManejadorDeComando<GuardarRazonSocialCommand, RazonSocialDto> manejador,
        CancellationToken cancellationToken)
    {
        RazonSocialDto razon = await manejador.EjecutarAsync(new GuardarRazonSocialCommand(null, peticion), cancellationToken);
        return TypedResults.Created(RutasApi.Recurso(RutasApi.RazonesSociales, razon.Id), razon);
    }

    private static async Task<Ok<RazonSocialDto>> ActualizarRazonSocialAsync(
        Guid razonSocialId,
        GuardarRazonSocialRequest peticion,
        IManejadorDeComando<GuardarRazonSocialCommand, RazonSocialDto> manejador,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarRazonSocialCommand(razonSocialId, peticion), cancellationToken));
}
