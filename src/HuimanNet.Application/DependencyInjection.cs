using HuimanNet.Application.Auditoria.Queries;
using HuimanNet.Application.Catalogos;
using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Application.Documentos.Queries;
using HuimanNet.Application.Documentos.Validators;
using HuimanNet.Application.Empleados;
using HuimanNet.Application.Empresas;
using HuimanNet.Application.Empresas.Queries;
using HuimanNet.Application.Incidencias;
using HuimanNet.Application.Inicio;
using HuimanNet.Application.Nomina;
using HuimanNet.Application.Notificaciones;
using HuimanNet.Application.Periodos.Commands;
using HuimanNet.Application.Periodos.Queries;
using HuimanNet.Application.Periodos.Validators;
using HuimanNet.Application.RazonesSociales;
using HuimanNet.Application.Usuarios;
using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.Application;

/// <summary>
/// Registro de los casos de uso y servicios de dominio en el contenedor de
/// dependencias.
/// </summary>
/// <remarks>
/// El registro es <b>explícito y sin escaneo de ensamblados</b>: la exploración
/// por reflexión no sobrevive al recorte de Native AOT. Cada manejador nuevo
/// debe añadirse aquí a mano; el coste es trivial y a cambio el grafo de
/// dependencias es completamente estático y verificable en compilación.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registra la capa de aplicación y los servicios de dominio.
    /// </summary>
    /// <param name="services">Colección de servicios del anfitrión.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="services"/> es <c>null</c>.
    /// </exception>
    /// <remarks>
    /// La política de carga concreta la registra la capa de infraestructura a
    /// partir de la configuración; aquí sólo se garantiza que exista un valor
    /// por defecto si nadie la sobrescribe.
    /// </remarks>
    public static IServiceCollection AgregarCapaDeAplicacion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Servicios de dominio: sin estado y deterministas, se comparten como singleton.
        services.AddSingleton<PoliticaDeAcceso>();
        services.AddSingleton<MotorDeCalculo>();

        // Valor por defecto de la política de carga. La capa de infraestructura
        // registra después la política leída de configuración, que prevalece:
        // en el contenedor, el último registro de un servicio es el que se resuelve.
        services.AddSingleton(PoliticaDeCarga.Predeterminada);

        services.AddSingleton<ValidadorDeDocumento>();
        services.AddSingleton(TimeProvider.System);

        // Utilidades con ámbito de petición.
        services.AddScoped<AutorizadorDeCasosDeUso>();
        services.AddScoped<RegistradorDeAuditoria>();
        services.AddScoped<ConstructorDePlanDeCalculo>();
        services.AddScoped<ArchivosDelPeriodo>();

        // Validadores de entrada.
        services.AddSingleton<IValidadorDeEntrada<SolicitarCargaDocumentoCommand>, ValidadorDeSolicitudDeCarga>();
        services.AddSingleton<IValidadorDeEntrada<ConfirmarCargaCommand>, ValidadorDeConfirmacionDeCarga>();
        services.AddSingleton<IValidadorDeEntrada<AbrirPeriodoCommand>, ValidadorDeAperturaDePeriodo>();

        RegistrarDocumentosYPeriodos(services);
        RegistrarCatalogosDeEmpresa(services);
        RegistrarNomina(services);
        RegistrarCatalogosDeCalculo(services);
        RegistrarUsuarios(services);

        return services;
    }

    /// <summary>
    /// Registra los casos de uso de documentos, períodos, empresas, bitácora,
    /// inicio y avisos.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void RegistrarDocumentosYPeriodos(IServiceCollection services)
    {
        services.AddScoped<IManejadorDeComando<SolicitarCargaDocumentoCommand, SolicitarCargaResponse>, SolicitarCargaDocumentoHandler>();
        services.AddScoped<IManejadorDeComando<ConfirmarCargaCommand, DocumentoDto>, ConfirmarCargaHandler>();
        services.AddScoped<IManejadorDeComando<RegistrarResultadoEscaneoCommand>, RegistrarResultadoEscaneoHandler>();
        services.AddScoped<IManejadorDeComando<AbrirPeriodoCommand, PeriodoDto>, AbrirPeriodoHandler>();
        services.AddScoped<IManejadorDeComando<CambiarEstadoPeriodoCommand>, CambiarEstadoPeriodoHandler>();

        services.AddScoped<IManejadorDeConsulta<ListarDocumentosPorPeriodoQuery, IReadOnlyList<DocumentoDto>>, ListarDocumentosPorPeriodoHandler>();
        services.AddScoped<IManejadorDeConsulta<ObtenerEnlaceDescargaQuery, EnlaceDescargaResponse>, ObtenerEnlaceDescargaHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>>, ListarPeriodosHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarBandejaOperadorQuery, IReadOnlyList<PeriodoDto>>, ListarBandejaOperadorHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarEmpresasQuery, IReadOnlyList<EmpresaDto>>, ListarEmpresasHandler>();
        services.AddScoped<IManejadorDeConsulta<ConsultarBitacoraQuery, PaginaDto<RegistroAuditoriaDto>>, ConsultarBitacoraHandler>();
        services.AddScoped<IManejadorDeConsulta<ObtenerResumenDeInicioQuery, ResumenDeInicioDto>, ObtenerResumenDeInicioHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarDestinatariosDeAvisoQuery, IReadOnlyList<string>>, ListarDestinatariosDeAvisoHandler>();
    }

    /// <summary>
    /// Registra los casos de uso de empresas, razones sociales, empleados y contratos.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <remarks>
    /// Un manejador que atiende varios casos de uso se registra una vez y se
    /// expone por cada interfaz, para compartir la instancia dentro del ámbito.
    /// </remarks>
    private static void RegistrarCatalogosDeEmpresa(IServiceCollection services)
    {
        services.AddScoped<GuardarEmpresaHandler>();
        services.AddScoped<IManejadorDeComando<CrearEmpresaCommand, EmpresaDetalleDto>>(sp => sp.GetRequiredService<GuardarEmpresaHandler>());
        services.AddScoped<IManejadorDeComando<ActualizarEmpresaCommand, EmpresaDetalleDto>>(sp => sp.GetRequiredService<GuardarEmpresaHandler>());

        services.AddScoped<ConsultarEmpresasHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarEmpresasDetalleQuery, IReadOnlyList<EmpresaDetalleDto>>>(sp => sp.GetRequiredService<ConsultarEmpresasHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerEmpresaQuery, EmpresaDetalleDto>>(sp => sp.GetRequiredService<ConsultarEmpresasHandler>());

        services.AddScoped<IManejadorDeComando<GuardarRazonSocialCommand, RazonSocialDto>, GuardarRazonSocialHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarRazonesSocialesQuery, IReadOnlyList<RazonSocialDto>>, ListarRazonesSocialesHandler>();

        services.AddScoped<GuardarEmpleadoHandler>();
        services.AddScoped<IManejadorDeComando<GuardarEmpleadoCommand, EmpleadoDto>>(sp => sp.GetRequiredService<GuardarEmpleadoHandler>());
        services.AddScoped<IManejadorDeComando<GuardarContratoCommand, ContratoDto>>(sp => sp.GetRequiredService<GuardarEmpleadoHandler>());

        services.AddScoped<ConsultarEmpleadosHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>>>(sp => sp.GetRequiredService<ConsultarEmpleadosHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerEmpleadoQuery, EmpleadoDto>>(sp => sp.GetRequiredService<ConsultarEmpleadosHandler>());
    }

    /// <summary>
    /// Registra los casos de uso de incidencias y del cálculo, la consulta y el
    /// cotejo de la nómina.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void RegistrarNomina(IServiceCollection services)
    {
        services.AddScoped<GuardarIncidenciaHandler>();
        services.AddScoped<IManejadorDeComando<GuardarIncidenciaCommand, IncidenciaDto>>(sp => sp.GetRequiredService<GuardarIncidenciaHandler>());
        services.AddScoped<IManejadorDeComando<EliminarIncidenciaCommand>>(sp => sp.GetRequiredService<GuardarIncidenciaHandler>());
        services.AddScoped<IManejadorDeComando<ImportarIncidenciasCommand, ResultadoDeImportacionDto>, ImportarIncidenciasHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarIncidenciasQuery, IReadOnlyList<IncidenciaDto>>, ListarIncidenciasHandler>();

        services.AddScoped<IManejadorDeComando<CalcularNominaCommand, CorridaDeNominaDto>, CalcularNominaHandler>();
        services.AddScoped<IManejadorDeComando<CambiarEstadoCorridaCommand>, CambiarEstadoCorridaHandler>();

        services.AddScoped<ConsultarNominaHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>>>(sp => sp.GetRequiredService<ConsultarNominaHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerResumenDeCorridaQuery, ResumenDeCorridaDto>>(sp => sp.GetRequiredService<ConsultarNominaHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerDetalleDeResultadoQuery, DetalleDeResultadoDto>>(sp => sp.GetRequiredService<ConsultarNominaHandler>());
        services.AddScoped<IManejadorDeConsulta<ExportarCorridaQuery, ArchivoExportado>>(sp => sp.GetRequiredService<ConsultarNominaHandler>());
        services.AddScoped<IManejadorDeConsulta<ListarCorridasRecientesQuery, IReadOnlyList<CorridaDeNominaDto>>, ListarCorridasRecientesHandler>();

        services.AddScoped<IManejadorDeComando<CotejarNominaCommand, CotejoDto>, CotejarNominaHandler>();
        services.AddScoped<ConsultarCotejosHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarCotejosQuery, IReadOnlyList<CotejoDto>>>(sp => sp.GetRequiredService<ConsultarCotejosHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerCotejoQuery, CotejoDto>>(sp => sp.GetRequiredService<ConsultarCotejosHandler>());
    }

    /// <summary>
    /// Registra los casos de uso de parámetros, tablas, conceptos y
    /// explicaciones del cálculo.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void RegistrarCatalogosDeCalculo(IServiceCollection services)
    {
        services.AddScoped<AdministrarCatalogosHandler>();
        services.AddScoped<IManejadorDeComando<GuardarParametroCommand, ParametroDeCalculoDto>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<EliminarParametroCommand>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<GuardarTablaCommand, TablaDeRangosDto>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<EliminarTablaCommand>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<GuardarConceptoCommand, ConceptoDeNominaDto>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<EliminarConceptoCommand>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<GuardarExplicacionCommand, ExplicacionDeCalculoDto>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());
        services.AddScoped<IManejadorDeComando<EliminarExplicacionCommand>>(sp => sp.GetRequiredService<AdministrarCatalogosHandler>());

        services.AddScoped<ConsultarCatalogosHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarParametrosQuery, IReadOnlyList<ParametroDeCalculoDto>>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
        services.AddScoped<IManejadorDeConsulta<ListarTablasQuery, IReadOnlyList<TablaDeRangosDto>>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
        services.AddScoped<IManejadorDeConsulta<ListarConceptosQuery, IReadOnlyList<ConceptoDeNominaDto>>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
        services.AddScoped<IManejadorDeConsulta<ProbarFormulaQuery, ProbarFormulaResponse>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
        services.AddScoped<IManejadorDeConsulta<ListarExplicacionesQuery, IReadOnlyList<ExplicacionDeCalculoDto>>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerExplicacionCompletaQuery, ExplicacionCompletaDto>>(sp => sp.GetRequiredService<ConsultarCatalogosHandler>());
    }

    /// <summary>Registra los casos de uso de usuarios y de la sesión.</summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void RegistrarUsuarios(IServiceCollection services)
    {
        services.AddScoped<AdministrarUsuariosHandler>();
        services.AddScoped<IManejadorDeComando<GuardarUsuarioCommand, UsuarioDto>>(sp => sp.GetRequiredService<AdministrarUsuariosHandler>());
        services.AddScoped<IManejadorDeComando<RestablecerContrasenaCommand>>(sp => sp.GetRequiredService<AdministrarUsuariosHandler>());

        services.AddScoped<ConsultarUsuariosHandler>();
        services.AddScoped<IManejadorDeConsulta<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>>(sp => sp.GetRequiredService<ConsultarUsuariosHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerUsuarioQuery, UsuarioDto>>(sp => sp.GetRequiredService<ConsultarUsuariosHandler>());

        services.AddScoped<ResolutorDeUsuarioAutenticado>();

        services.AddScoped<SesionDeUsuarioHandler>();
        services.AddScoped<IManejadorDeComando<IniciarSesionLocalCommand, IniciarSesionResponse>>(sp => sp.GetRequiredService<SesionDeUsuarioHandler>());
        services.AddScoped<IManejadorDeComando<ValidarCredencialesLocalesCommand, SesionLocalValidada>>(sp => sp.GetRequiredService<SesionDeUsuarioHandler>());
        services.AddScoped<IManejadorDeComando<CambiarContrasenaCommand>>(sp => sp.GetRequiredService<SesionDeUsuarioHandler>());
        services.AddScoped<IManejadorDeComando<ActualizarPreferenciasCommand>>(sp => sp.GetRequiredService<SesionDeUsuarioHandler>());
        services.AddScoped<IManejadorDeConsulta<ObtenerUsuarioActualQuery, UsuarioActualDto>>(sp => sp.GetRequiredService<SesionDeUsuarioHandler>());
    }
}
