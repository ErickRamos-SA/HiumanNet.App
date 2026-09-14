using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.App.Views;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App;

/// <summary>
/// Navegación de la app: pantalla de sesión, barra inferior y rutas de detalle.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Ruta de la pantalla de sesión.</summary>
    public const string RutaSesion = "//sesion";

    /// <summary>Ruta de la pestaña de inicio.</summary>
    public const string RutaInicio = "//portal/inicio";

    /// <summary>Ruta de la pestaña de períodos.</summary>
    public const string RutaPeriodos = "//portal/periodos";

    /// <summary>Ruta de la pestaña de incidencias.</summary>
    public const string RutaIncidencias = "//portal/incidencias";

    /// <summary>Ruta de la pestaña de nómina.</summary>
    public const string RutaNomina = "//portal/nomina";

    /// <summary>Ruta de la pestaña «Más».</summary>
    public const string RutaMas = "//portal/mas";

    /// <summary>Ruta relativa de los documentos de un período.</summary>
    public const string RutaDocumentos = "documentos";

    /// <summary>Ruta relativa de la captura de una incidencia.</summary>
    public const string RutaIncidencia = "incidencia";

    /// <summary>Ruta relativa del resumen de una corrida.</summary>
    public const string RutaCorrida = "corrida";

    /// <summary>Ruta relativa del detalle de un trabajador.</summary>
    public const string RutaResultado = "resultado";

    /// <summary>Ruta relativa de la lista de empleados.</summary>
    public const string RutaEmpleados = "empleados";

    /// <summary>Ruta relativa de la ficha de un empleado.</summary>
    public const string RutaEmpleado = "empleado";

    /// <summary>Ruta relativa de la explicación de cálculos.</summary>
    public const string RutaExplicacion = "explicacion";

    /// <summary>Ruta relativa de la administración.</summary>
    public const string RutaAdministracion = "administracion";

    /// <summary>Ruta relativa de la cuenta.</summary>
    public const string RutaCuenta = "cuenta";

    private readonly SesionDeLaApp _sesion;
    private readonly Traductor _traductor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AppShell"/>.
    /// </summary>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="traductor">Traductor.</param>
    public AppShell(SesionDeLaApp sesion, Traductor traductor)
    {
        _sesion = sesion;
        _traductor = traductor;

        InitializeComponent();

        Routing.RegisterRoute(RutaDocumentos, typeof(DocumentosPage));
        Routing.RegisterRoute(RutaIncidencia, typeof(IncidenciaPage));
        Routing.RegisterRoute(RutaCorrida, typeof(CorridaPage));
        Routing.RegisterRoute(RutaResultado, typeof(ResultadoPage));
        Routing.RegisterRoute(RutaEmpleados, typeof(EmpleadosPage));
        Routing.RegisterRoute(RutaEmpleado, typeof(EmpleadoPage));
        Routing.RegisterRoute(RutaExplicacion, typeof(ExplicacionPage));
        Routing.RegisterRoute(RutaAdministracion, typeof(AdministracionPage));
        Routing.RegisterRoute(RutaCuenta, typeof(CuentaPage));

        ActualizarTitulos();

        WeakReferenceMessenger.Default.Register<AppShell, IdiomaCambiadoMensaje>(this, static (shell, _) => shell.ActualizarTitulos());
        WeakReferenceMessenger.Default.Register<AppShell, SesionIniciadaMensaje>(this, static (shell, _) => shell.AplicarPermisos());
    }

    private void ActualizarTitulos()
    {
        TabInicio.Title = _traductor["nav.inicio"];
        TabPeriodos.Title = _traductor["periodos.titulo"];
        TabIncidencias.Title = _traductor["nav.incidencias"];
        TabNomina.Title = _traductor["nav.nomina"];
        TabMas.Title = _traductor["movil.mas"];
    }

    private void AplicarPermisos()
    {
        TabPeriodos.IsVisible = _sesion.Puede(AccionDelSistema.CargarDocumentos) || _sesion.Puede(AccionDelSistema.DescargarDocumentos);
        TabIncidencias.IsVisible = _sesion.Puede(AccionDelSistema.CapturarIncidencias);
        TabNomina.IsVisible = _sesion.Puede(AccionDelSistema.ConsultarNomina);
    }
}
