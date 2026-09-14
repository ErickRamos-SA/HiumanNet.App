using HuimanNet.App.Configuracion;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.App.ViewModels;
using HuimanNet.App.Views;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace HuimanNet.App;

/// <summary>
/// Composición de la app móvil: servicios, cliente HTTP, ViewModel y vistas.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Construye la aplicación.
    /// </summary>
    /// <returns>La aplicación configurada.</returns>
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        // Se usa la tipografía del sistema: evita empaquetar fuentes y respeta
        // los ajustes de accesibilidad del dispositivo.
        builder.UseMauiApp<App>();

        // -------------------------------------------------------------------
        // Estado de la sesión, idioma e identidad.
        // -------------------------------------------------------------------
        builder.Services.AddSingleton(_ => Textos.Traductor);
        builder.Services.AddSingleton<SesionDeLaApp>();
        builder.Services.AddSingleton<ProveedorDeTokenLocal>();
        builder.Services.AddSingleton<ProveedorDeTokenEntra>();
        builder.Services.AddSingleton<IProveedorDeToken, ProveedorDeToken>();
        builder.Services.AddTransient<ManejadorDeAutenticacion>();

        // -------------------------------------------------------------------
        // Cliente de la API con token y resiliencia. Los reintentos se limitan
        // a métodos seguros: repetir un POST de cálculo duplicaría la corrida.
        // Los tiempos son holgados porque calcular una empresa grande tarda.
        // -------------------------------------------------------------------
        builder.Services
            .AddHttpClient<IServicioDeApi, ServicioDeApi>(cliente =>
            {
                cliente.BaseAddress = new Uri(OpcionesDeLaApp.UrlBaseApi);
                cliente.Timeout = OpcionesDeLaApp.TiempoDeEspera;
            })
            .AddHttpMessageHandler<ManejadorDeAutenticacion>()
            .AddStandardResilienceHandler(opciones =>
            {
                opciones.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                opciones.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(4);
                opciones.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(5);
                opciones.Retry.DisableForUnsafeHttpMethods();
            });

        // -------------------------------------------------------------------
        // Servicios de plataforma desacoplados de las ViewModel.
        // -------------------------------------------------------------------
        builder.Services.AddSingleton<IServicioDeNavegacion, ServicioDeNavegacionShell>();
        builder.Services.AddSingleton<IServicioDeDialogos, ServicioDeDialogos>();

        // -------------------------------------------------------------------
        // Shell, ViewModel y vistas.
        // -------------------------------------------------------------------
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<SesionViewModel>();
        builder.Services.AddTransient<InicioViewModel>();
        builder.Services.AddTransient<PeriodosViewModel>();
        builder.Services.AddTransient<DocumentosViewModel>();
        builder.Services.AddTransient<IncidenciasViewModel>();
        builder.Services.AddTransient<IncidenciaViewModel>();
        builder.Services.AddTransient<NominaViewModel>();
        builder.Services.AddTransient<CorridaViewModel>();
        builder.Services.AddTransient<ResultadoViewModel>();
        builder.Services.AddTransient<EmpleadosViewModel>();
        builder.Services.AddTransient<EmpleadoViewModel>();
        builder.Services.AddTransient<ExplicacionViewModel>();
        builder.Services.AddTransient<AdministracionViewModel>();
        builder.Services.AddTransient<MasViewModel>();
        builder.Services.AddTransient<CuentaViewModel>();

        builder.Services.AddTransient<SesionPage>();
        builder.Services.AddTransient<InicioPage>();
        builder.Services.AddTransient<PeriodosPage>();
        builder.Services.AddTransient<DocumentosPage>();
        builder.Services.AddTransient<IncidenciasPage>();
        builder.Services.AddTransient<IncidenciaPage>();
        builder.Services.AddTransient<NominaPage>();
        builder.Services.AddTransient<CorridaPage>();
        builder.Services.AddTransient<ResultadoPage>();
        builder.Services.AddTransient<EmpleadosPage>();
        builder.Services.AddTransient<EmpleadoPage>();
        builder.Services.AddTransient<ExplicacionPage>();
        builder.Services.AddTransient<AdministracionPage>();
        builder.Services.AddTransient<MasPage>();
        builder.Services.AddTransient<CuentaPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
