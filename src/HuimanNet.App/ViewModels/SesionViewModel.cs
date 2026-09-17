using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Inicio de sesión: con cuenta de Microsoft (modo Entra) o con correo y
/// contraseña (modo local), según lo que declare el servidor.
/// </summary>
public sealed partial class SesionViewModel : ViewModelBase
{
    /// <summary>Tiempo máximo para comprobar el servidor al abrir la app.</summary>
    private static readonly TimeSpan TiempoMaximoDeComprobacion = TimeSpan.FromSeconds(15);

    private readonly IServicioDeApi _api;
    private readonly IProveedorDeToken _token;
    private readonly ProveedorDeTokenLocal _tokenLocal;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SesionViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="token">Proveedor de token.</param>
    /// <param name="tokenLocal">Token del modo local.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public SesionViewModel(
        Traductor traductor, IServicioDeApi api, IProveedorDeToken token, ProveedorDeTokenLocal tokenLocal,
        SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _token = token;
        _tokenLocal = tokenLocal;
        _sesion = sesion;
        _navegacion = navegacion;
        EsLocal = sesion.EsLocal;
    }

    /// <summary>Obtiene o establece si se está comprobando una sesión previa.</summary>
    /// <value><c>true</c> al abrir la app.</value>
    [ObservableProperty]
    public partial bool ComprobandoSesion { get; set; } = true;

    /// <summary>Obtiene o establece si el servidor usa cuentas locales.</summary>
    /// <value><c>true</c> para mostrar el formulario de correo y contraseña.</value>
    [ObservableProperty]
    public partial bool EsLocal { get; set; }

    /// <summary>Obtiene o establece el correo escrito.</summary>
    /// <value>Correo del usuario.</value>
    [ObservableProperty]
    public partial string? Correo { get; set; }

    /// <summary>Obtiene o establece la contraseña escrita; se borra tras iniciar sesión.</summary>
    /// <value>Contraseña en claro, sólo en memoria.</value>
    [ObservableProperty]
    public partial string? Contrasena { get; set; }

    /// <summary>
    /// Consulta el modo de identidad del servidor y reanuda la sesión si hay un token vigente.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task ComprobarSesionAsync()
    {
        ComprobandoSesion = true;

        await EjecutarAsync(async () =>
        {
            // Consulta corta: si la API no responde se avisa en segundos, en lugar
            // de esperar el tiempo máximo pensado para operaciones largas.
            ConfiguracionPublicaDto configuracion;

            using (var limite = new CancellationTokenSource(TiempoMaximoDeComprobacion))
            {
                try
                {
                    configuracion = await _api.ObtenerConfiguracionAsync(limite.Token);
                }
                catch (OperationCanceledException) when (limite.IsCancellationRequested)
                {
                    MensajeDeError = T["movil.sinConexion"];
                    return;
                }
            }

            _sesion.ModoDeIdentidad = configuracion.ModoDeIdentidad;
            EsLocal = _sesion.EsLocal;

            if (!string.IsNullOrEmpty(await _token.ObtenerTokenSilenciosoAsync()))
            {
                await EntrarAsync();
            }
        });

        ComprobandoSesion = false;
    }

    /// <summary>
    /// Inicia sesión con el mecanismo del servidor.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task IniciarSesionAsync() => EjecutarAsync(async () =>
    {
        if (EsLocal)
        {
            if (string.IsNullOrWhiteSpace(Correo) || string.IsNullOrEmpty(Contrasena))
            {
                MensajeDeError = T["acceso.credencialesInvalidas"];
                return;
            }

            try
            {
                IniciarSesionResponse respuesta = await _api.IniciarSesionLocalAsync(new IniciarSesionRequest(Correo.Trim(), Contrasena));
                await _tokenLocal.EstablecerAsync(respuesta.Token, respuesta.ExpiraEn);
            }
            catch (ErrorDeApiException excepcion) when (excepcion.Estado is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            {
                MensajeDeError = T["acceso.credencialesInvalidas"];
                return;
            }
            finally
            {
                Contrasena = null;
            }
        }
        else
        {
            await _token.IniciarSesionAsync();
        }

        await EntrarAsync();
    });

    /// <summary>
    /// Carga la identidad y las empresas elegibles, inicia la sesión de la app
    /// y navega al inicio.
    /// </summary>
    /// <returns>Tarea que finaliza al navegar.</returns>
    private async Task EntrarAsync()
    {
        UsuarioActualDto usuario = await _api.ObtenerUsuarioActualAsync();
        // Nómina y administración eligen entre todas; la empresa cliente, entre las suyas.
        IReadOnlyList<EmpresaDto> empresas = usuario.EsTransversal ? await _api.ListarEmpresasAsync() : usuario.Empresas;

        _sesion.Iniciar(usuario, empresas);
        await _navegacion.IrAAsync(AppShell.RutaInicio);

        if (usuario.RequiereCambioDeContrasena)
        {
            await _navegacion.IrAAsync(AppShell.RutaCuenta);
        }
    }
}
