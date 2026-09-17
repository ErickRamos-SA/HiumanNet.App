using HuimanNet.Api.Seguridad;
using HuimanNet.Application.Catalogos;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Usuarios;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HuimanNet.Api.Endpoints;

/// <summary>
/// Endpoints de los catálogos de cálculo (parámetros, tablas, conceptos y
/// explicaciones) y de la administración de usuarios.
/// </summary>
/// <remarks>
/// Los grupos exigen sólo un usuario del portal: el permiso concreto
/// (administrar catálogos, consultar la explicación, administrar usuarios) lo
/// verifica cada caso de uso contra los permisos habilitados en la base de datos.
/// </remarks>
public static class CatalogosEndpoints
{
    /// <summary>
    /// Mapea los endpoints de catálogos y usuarios.
    /// </summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeCatalogos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder parametros = Grupo(app, RutasApi.Parametros, "Catálogos de cálculo");
        parametros.MapGet("/", ListarParametrosAsync).WithName("ListarParametros");
        parametros.MapPost("/", CrearParametroAsync).WithName("CrearParametro").ProducesProblem(StatusCodes.Status400BadRequest);
        parametros.MapPut("/{parametroId:guid}", ActualizarParametroAsync).WithName("ActualizarParametro").ProducesProblem(StatusCodes.Status400BadRequest);
        parametros.MapDelete("/{parametroId:guid}", EliminarParametroAsync).WithName("EliminarParametro");

        RouteGroupBuilder tablas = Grupo(app, RutasApi.Tablas, "Catálogos de cálculo");
        tablas.MapGet("/", ListarTablasAsync).WithName("ListarTablas");
        tablas.MapPost("/", CrearTablaAsync).WithName("CrearTabla").ProducesProblem(StatusCodes.Status400BadRequest);
        tablas.MapPut("/{tablaId:guid}", ActualizarTablaAsync).WithName("ActualizarTabla").ProducesProblem(StatusCodes.Status400BadRequest);
        tablas.MapDelete("/{tablaId:guid}", EliminarTablaAsync).WithName("EliminarTabla");

        RouteGroupBuilder conceptos = Grupo(app, RutasApi.Conceptos, "Catálogos de cálculo");
        conceptos.MapGet("/", ListarConceptosAsync).WithName("ListarConceptos");
        conceptos.MapPost("/", CrearConceptoAsync).WithName("CrearConcepto").ProducesProblem(StatusCodes.Status400BadRequest);
        conceptos.MapPut("/{conceptoId:guid}", ActualizarConceptoAsync).WithName("ActualizarConcepto").ProducesProblem(StatusCodes.Status400BadRequest);
        conceptos.MapDelete("/{conceptoId:guid}", EliminarConceptoAsync).WithName("EliminarConcepto");
        conceptos.MapPost("/probar", ProbarFormulaAsync)
            .WithName("ProbarFormula")
            .WithSummary("Compila y evalúa una fórmula con valores de prueba, sin guardar nada.");

        RouteGroupBuilder explicaciones = Grupo(app, RutasApi.Explicaciones, "Explicación de cálculos");
        explicaciones.MapGet("/", ListarExplicacionesAsync).WithName("ListarExplicaciones");
        explicaciones.MapGet("/completa", ObtenerExplicacionCompletaAsync)
            .WithName("ObtenerExplicacionCompleta")
            .WithSummary("Explicación de un esquema: narrativa, conceptos con fórmula, parámetros, tablas, variables y funciones.");
        explicaciones.MapPost("/", CrearExplicacionAsync).WithName("CrearExplicacion").ProducesProblem(StatusCodes.Status400BadRequest);
        explicaciones.MapPut("/{explicacionId:guid}", ActualizarExplicacionAsync).WithName("ActualizarExplicacion").ProducesProblem(StatusCodes.Status400BadRequest);
        explicaciones.MapDelete("/{explicacionId:guid}", EliminarExplicacionAsync).WithName("EliminarExplicacion");

        RouteGroupBuilder usuarios = Grupo(app, RutasApi.Usuarios, "Usuarios");
        usuarios.MapGet("/", ListarUsuariosAsync).WithName("ListarUsuarios");
        usuarios.MapGet("/{usuarioId:guid}", ObtenerUsuarioAsync).WithName("ObtenerUsuario");
        usuarios.MapPost("/", CrearUsuarioAsync).WithName("CrearUsuario").ProducesProblem(StatusCodes.Status400BadRequest);
        usuarios.MapPut("/{usuarioId:guid}", ActualizarUsuarioAsync).WithName("ActualizarUsuario").ProducesProblem(StatusCodes.Status400BadRequest);
        usuarios.MapPut("/{usuarioId:guid}/contrasena", RestablecerContrasenaAsync)
            .WithName("RestablecerContrasena")
            .WithSummary("Asigna una contraseña temporal; el usuario deberá cambiarla al entrar.");

        return app;
    }

    /// <summary>Crea un grupo de rutas que exige un usuario del portal.</summary>
    /// <param name="app">Constructor de rutas.</param>
    /// <param name="ruta">Prefijo del grupo.</param>
    /// <param name="etiqueta">Etiqueta de OpenAPI.</param>
    /// <returns>El grupo de rutas.</returns>
    private static RouteGroupBuilder Grupo(IEndpointRouteBuilder app, string ruta, string etiqueta)
        => app.MapGroup(ruta).WithTags(etiqueta).RequireAuthorization(PoliticasDeAutorizacion.UsuarioDelPortal);

    // --- Parámetros -------------------------------------------------------

    /// <summary>Lista los parámetros de cálculo.</summary>
    /// <param name="empresaId">Empresa cuyo catálogo se consulta; <c>null</c> para el catálogo general.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los parámetros.</returns>
    private static async Task<Ok<IReadOnlyList<ParametroDeCalculoDto>>> ListarParametrosAsync(
        Guid? empresaId, IManejadorDeConsulta<ListarParametrosQuery, IReadOnlyList<ParametroDeCalculoDto>> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarParametrosQuery(empresaId), cancellationToken));

    /// <summary>Crea un parámetro de cálculo.</summary>
    /// <param name="peticion">Datos del parámetro.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el parámetro creado.</returns>
    private static async Task<Ok<ParametroDeCalculoDto>> CrearParametroAsync(
        GuardarParametroRequest peticion, IManejadorDeComando<GuardarParametroCommand, ParametroDeCalculoDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarParametroCommand(null, peticion), cancellationToken));

    /// <summary>Actualiza un parámetro de cálculo.</summary>
    /// <param name="parametroId">Parámetro a actualizar.</param>
    /// <param name="peticion">Datos nuevos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el parámetro actualizado.</returns>
    private static async Task<Ok<ParametroDeCalculoDto>> ActualizarParametroAsync(
        Guid parametroId, GuardarParametroRequest peticion, IManejadorDeComando<GuardarParametroCommand, ParametroDeCalculoDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarParametroCommand(parametroId, peticion), cancellationToken));

    /// <summary>Elimina un parámetro de cálculo.</summary>
    /// <param name="parametroId">Parámetro a eliminar.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se eliminó.</returns>
    private static async Task<NoContent> EliminarParametroAsync(
        Guid parametroId, IManejadorDeComando<EliminarParametroCommand> manejador, CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new EliminarParametroCommand(parametroId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Tablas -----------------------------------------------------------

    /// <summary>Lista las tablas de rangos.</summary>
    /// <param name="empresaId">Empresa cuyo catálogo se consulta; <c>null</c> para el catálogo general.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con las tablas.</returns>
    private static async Task<Ok<IReadOnlyList<TablaDeRangosDto>>> ListarTablasAsync(
        Guid? empresaId, IManejadorDeConsulta<ListarTablasQuery, IReadOnlyList<TablaDeRangosDto>> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarTablasQuery(empresaId), cancellationToken));

    /// <summary>Crea una tabla de rangos.</summary>
    /// <param name="peticion">Datos de la tabla y sus rangos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la tabla creada.</returns>
    private static async Task<Ok<TablaDeRangosDto>> CrearTablaAsync(
        GuardarTablaRequest peticion, IManejadorDeComando<GuardarTablaCommand, TablaDeRangosDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarTablaCommand(null, peticion), cancellationToken));

    /// <summary>Actualiza una tabla de rangos.</summary>
    /// <param name="tablaId">Tabla a actualizar.</param>
    /// <param name="peticion">Datos nuevos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la tabla actualizada.</returns>
    private static async Task<Ok<TablaDeRangosDto>> ActualizarTablaAsync(
        Guid tablaId, GuardarTablaRequest peticion, IManejadorDeComando<GuardarTablaCommand, TablaDeRangosDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarTablaCommand(tablaId, peticion), cancellationToken));

    /// <summary>Elimina una tabla de rangos.</summary>
    /// <param name="tablaId">Tabla a eliminar.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se eliminó.</returns>
    private static async Task<NoContent> EliminarTablaAsync(
        Guid tablaId, IManejadorDeComando<EliminarTablaCommand> manejador, CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new EliminarTablaCommand(tablaId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Conceptos --------------------------------------------------------

    /// <summary>Lista los conceptos de nómina.</summary>
    /// <param name="empresaId">Empresa cuyo catálogo se consulta; <c>null</c> para el catálogo general.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los conceptos.</returns>
    private static async Task<Ok<IReadOnlyList<ConceptoDeNominaDto>>> ListarConceptosAsync(
        Guid? empresaId, IManejadorDeConsulta<ListarConceptosQuery, IReadOnlyList<ConceptoDeNominaDto>> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarConceptosQuery(empresaId), cancellationToken));

    /// <summary>Crea un concepto de nómina.</summary>
    /// <param name="peticion">Datos del concepto y su fórmula.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el concepto creado.</returns>
    private static async Task<Ok<ConceptoDeNominaDto>> CrearConceptoAsync(
        GuardarConceptoRequest peticion, IManejadorDeComando<GuardarConceptoCommand, ConceptoDeNominaDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarConceptoCommand(null, peticion), cancellationToken));

    /// <summary>Actualiza un concepto de nómina.</summary>
    /// <param name="conceptoId">Concepto a actualizar.</param>
    /// <param name="peticion">Datos nuevos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el concepto actualizado.</returns>
    private static async Task<Ok<ConceptoDeNominaDto>> ActualizarConceptoAsync(
        Guid conceptoId, GuardarConceptoRequest peticion, IManejadorDeComando<GuardarConceptoCommand, ConceptoDeNominaDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarConceptoCommand(conceptoId, peticion), cancellationToken));

    /// <summary>Elimina un concepto de nómina.</summary>
    /// <param name="conceptoId">Concepto a eliminar.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se eliminó.</returns>
    private static async Task<NoContent> EliminarConceptoAsync(
        Guid conceptoId, IManejadorDeComando<EliminarConceptoCommand> manejador, CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new EliminarConceptoCommand(conceptoId), cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>Compila y evalúa una fórmula con valores de prueba, sin guardar nada.</summary>
    /// <param name="peticion">Fórmula y valores de prueba.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el resultado o el error de la fórmula.</returns>
    private static async Task<Ok<ProbarFormulaResponse>> ProbarFormulaAsync(
        ProbarFormulaRequest peticion, IManejadorDeConsulta<ProbarFormulaQuery, ProbarFormulaResponse> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ProbarFormulaQuery(peticion), cancellationToken));

    // --- Explicaciones ----------------------------------------------------

    /// <summary>Lista las secciones de la explicación de los cálculos.</summary>
    /// <param name="esquema">Esquema a filtrar; todos si se omite.</param>
    /// <param name="idioma">Idioma a filtrar; todos si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con las secciones.</returns>
    private static async Task<Ok<IReadOnlyList<ExplicacionDeCalculoDto>>> ListarExplicacionesAsync(
        EsquemaDePago? esquema, Idioma? idioma,
        IManejadorDeConsulta<ListarExplicacionesQuery, IReadOnlyList<ExplicacionDeCalculoDto>> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarExplicacionesQuery(esquema, idioma), cancellationToken));

    /// <summary>
    /// Obtiene la explicación completa de un esquema: narrativa, conceptos con
    /// fórmula, parámetros, tablas, variables y funciones.
    /// </summary>
    /// <param name="esquema">Esquema a explicar.</param>
    /// <param name="idioma">Idioma de la narrativa; el del usuario si se omite.</param>
    /// <param name="empresaId">Empresa cuyo catálogo se explica; <c>null</c> para el catálogo general.</param>
    /// <param name="fecha">Fecha de vigencia del catálogo; la decide el caso de uso si se omite.</param>
    /// <param name="usuarioActual">Usuario autenticado, para el idioma predeterminado.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la explicación.</returns>
    private static async Task<Ok<ExplicacionCompletaDto>> ObtenerExplicacionCompletaAsync(
        EsquemaDePago esquema, Idioma? idioma, Guid? empresaId, DateOnly? fecha,
        IUsuarioActual usuarioActual,
        IManejadorDeConsulta<ObtenerExplicacionCompletaQuery, ExplicacionCompletaDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(
            new ObtenerExplicacionCompletaQuery(esquema, idioma ?? usuarioActual.Idioma, empresaId, fecha), cancellationToken));

    /// <summary>Crea una sección de la explicación de los cálculos.</summary>
    /// <param name="peticion">Datos de la sección.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la sección creada.</returns>
    private static async Task<Ok<ExplicacionDeCalculoDto>> CrearExplicacionAsync(
        GuardarExplicacionRequest peticion, IManejadorDeComando<GuardarExplicacionCommand, ExplicacionDeCalculoDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarExplicacionCommand(null, peticion), cancellationToken));

    /// <summary>Actualiza una sección de la explicación de los cálculos.</summary>
    /// <param name="explicacionId">Sección a actualizar.</param>
    /// <param name="peticion">Datos nuevos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con la sección actualizada.</returns>
    private static async Task<Ok<ExplicacionDeCalculoDto>> ActualizarExplicacionAsync(
        Guid explicacionId, GuardarExplicacionRequest peticion, IManejadorDeComando<GuardarExplicacionCommand, ExplicacionDeCalculoDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarExplicacionCommand(explicacionId, peticion), cancellationToken));

    /// <summary>Elimina una sección de la explicación de los cálculos.</summary>
    /// <param name="explicacionId">Sección a eliminar.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se eliminó.</returns>
    private static async Task<NoContent> EliminarExplicacionAsync(
        Guid explicacionId, IManejadorDeComando<EliminarExplicacionCommand> manejador, CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new EliminarExplicacionCommand(explicacionId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Usuarios ---------------------------------------------------------

    /// <summary>Lista los usuarios.</summary>
    /// <param name="empresaId">Empresa a filtrar; todas las visibles si se omite.</param>
    /// <param name="incluirInactivos">Incluye los desactivados; falso si se omite.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con los usuarios.</returns>
    private static async Task<Ok<IReadOnlyList<UsuarioDto>>> ListarUsuariosAsync(
        Guid? empresaId, bool? incluirInactivos,
        IManejadorDeConsulta<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ListarUsuariosQuery(empresaId, incluirInactivos ?? false), cancellationToken));

    /// <summary>Obtiene un usuario.</summary>
    /// <param name="usuarioId">Usuario consultado.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el usuario.</returns>
    private static async Task<Ok<UsuarioDto>> ObtenerUsuarioAsync(
        Guid usuarioId, IManejadorDeConsulta<ObtenerUsuarioQuery, UsuarioDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new ObtenerUsuarioQuery(usuarioId), cancellationToken));

    /// <summary>Crea un usuario.</summary>
    /// <param name="peticion">Datos del usuario, su rol y sus permisos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>201 con el usuario y su ubicación.</returns>
    private static async Task<Created<UsuarioDto>> CrearUsuarioAsync(
        GuardarUsuarioRequest peticion, IManejadorDeComando<GuardarUsuarioCommand, UsuarioDto> manejador, CancellationToken cancellationToken)
    {
        UsuarioDto usuario = await manejador.EjecutarAsync(new GuardarUsuarioCommand(null, peticion), cancellationToken);
        return TypedResults.Created(RutasApi.Recurso(RutasApi.Usuarios, usuario.Id), usuario);
    }

    /// <summary>Actualiza un usuario.</summary>
    /// <param name="usuarioId">Usuario a actualizar.</param>
    /// <param name="peticion">Datos nuevos.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>200 con el usuario actualizado.</returns>
    private static async Task<Ok<UsuarioDto>> ActualizarUsuarioAsync(
        Guid usuarioId, GuardarUsuarioRequest peticion, IManejadorDeComando<GuardarUsuarioCommand, UsuarioDto> manejador, CancellationToken cancellationToken)
        => TypedResults.Ok(await manejador.EjecutarAsync(new GuardarUsuarioCommand(usuarioId, peticion), cancellationToken));

    /// <summary>Asigna una contraseña temporal; el usuario deberá cambiarla al entrar.</summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="peticion">Contraseña temporal.</param>
    /// <param name="manejador">Caso de uso que atiende la petición.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>204 si se asignó.</returns>
    private static async Task<NoContent> RestablecerContrasenaAsync(
        Guid usuarioId, RestablecerContrasenaRequest peticion, IManejadorDeComando<RestablecerContrasenaCommand> manejador, CancellationToken cancellationToken)
    {
        await manejador.EjecutarAsync(new RestablecerContrasenaCommand(usuarioId, peticion.NuevaContrasena), cancellationToken);
        return TypedResults.NoContent();
    }
}
