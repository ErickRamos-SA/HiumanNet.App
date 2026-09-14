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

/// <summary>Indicador numérico del inicio.</summary>
/// <param name="Etiqueta">Texto traducido.</param>
/// <param name="Valor">Valor.</param>
public sealed record Indicador(string Etiqueta, int Valor);

/// <summary>Opción del menú «Más».</summary>
/// <param name="Titulo">Título traducido.</param>
/// <param name="Descripcion">Descripción traducida.</param>
/// <param name="Ruta">Ruta de Shell.</param>
public sealed record OpcionDeMenu(string Titulo, string Descripcion, string Ruta);

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

/// <summary>
/// Inicio: saludo, indicadores, pendientes y empresa de trabajo.
/// </summary>
public sealed partial class InicioViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="InicioViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public InicioViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["inicio.titulo"];
    }

    /// <summary>Obtiene los indicadores.</summary>
    /// <value>Empleados, períodos, documentos y corridas por cotejar.</value>
    public ObservableCollection<Indicador> Indicadores { get; } = [];

    /// <summary>Obtiene los pendientes del usuario.</summary>
    /// <value>Tareas sugeridas por el servidor.</value>
    public ObservableCollection<PendienteDto> Pendientes { get; } = [];

    /// <summary>Obtiene las empresas elegibles.</summary>
    /// <value>Todas las activas para nómina y administración; las suyas para la empresa cliente.</value>
    public IReadOnlyList<EmpresaDto> Empresas => _sesion.Empresas;

    /// <summary>Indica si el usuario elige la empresa de trabajo.</summary>
    /// <value><c>true</c> para mostrar el selector de empresa.</value>
    public bool EligeEmpresa => _sesion.EligeEmpresa;

    /// <summary>Obtiene los nombres de las empresas para el selector.</summary>
    /// <value>Razones sociales en el mismo orden que <see cref="Empresas"/>.</value>
    public IReadOnlyList<string> NombresDeEmpresas => [.. _sesion.Empresas.Select(e => e.RazonSocial)];

    /// <summary>Obtiene o establece la posición de la empresa elegida.</summary>
    /// <value>-1 si no hay empresa.</value>
    public int IndiceDeEmpresa
    {
        get => EmpresaSeleccionada is null ? -1 : _sesion.Empresas.ToList().FindIndex(e => e.Id == EmpresaSeleccionada.Id);
        set
        {
            if (value >= 0 && value < _sesion.Empresas.Count)
            {
                EmpresaSeleccionada = _sesion.Empresas[value];
            }
        }
    }

    /// <summary>Obtiene o establece la empresa de trabajo.</summary>
    /// <value>Empresa elegida.</value>
    [ObservableProperty]
    public partial EmpresaDto? EmpresaSeleccionada { get; set; }

    /// <summary>Obtiene o establece el saludo.</summary>
    /// <value>Texto traducido con el nombre.</value>
    [ObservableProperty]
    public partial string Saludo { get; set; } = string.Empty;

    /// <summary>Obtiene o establece si no hay pendientes.</summary>
    /// <value><c>true</c> para mostrar el estado vacío.</value>
    [ObservableProperty]
    public partial bool SinPendientes { get; set; }

    /// <summary>
    /// Carga indicadores y pendientes.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        if (_sesion.Usuario is not { } usuario)
        {
            await _navegacion.IrAAsync(AppShell.RutaSesion);
            return;
        }

        Titulo = T["inicio.titulo"];
        Saludo = T.Formato("inicio.saludo", usuario.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty);
        OnPropertyChanged(nameof(Empresas));
        OnPropertyChanged(nameof(NombresDeEmpresas));
        OnPropertyChanged(nameof(EligeEmpresa));
        EmpresaSeleccionada = _sesion.Empresa;

        // Nómina y administración ven el resumen de todas las empresas; la
        // empresa cliente, el de la empresa elegida (o su principal).
        ResumenDeInicioDto resumen = await _api.ObtenerResumenDeInicioAsync(_sesion.EsTransversal ? null : _sesion.EmpresaDeConsulta);

        Reemplazar(Indicadores,
        [
            new Indicador(T["inicio.empleadosActivos"], resumen.EmpleadosActivos),
            new Indicador(T["inicio.periodosAbiertos"], resumen.PeriodosAbiertos),
            new Indicador(T["inicio.documentosDisponibles"], resumen.DocumentosDisponibles),
            new Indicador(T["inicio.corridasPorCotejar"], resumen.CorridasPorCotejar),
        ]);

        Reemplazar(Pendientes, resumen.Pendientes);
        SinPendientes = Pendientes.Count == 0;
    });

    /// <summary>
    /// Abre la pantalla que resuelve un pendiente.
    /// </summary>
    /// <param name="pendiente">Pendiente elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirPendienteAsync(PendienteDto? pendiente)
        => pendiente is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(pendiente.Clave == "pendiente.cotejarCorrida" ? AppShell.RutaNomina : AppShell.RutaPeriodos);

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => CargarCommand.Execute(null);

    partial void OnEmpresaSeleccionadaChanged(EmpresaDto? value)
    {
        _sesion.SeleccionarEmpresa(value);
        OnPropertyChanged(nameof(IndiceDeEmpresa));

        // El resumen de la empresa cliente depende de la empresa elegida.
        if (!_sesion.EsTransversal)
        {
            CargarCommand.Execute(null);
        }
    }
}

/// <summary>
/// Menú «Más»: accesos a las pantallas que no caben en la barra inferior.
/// </summary>
public sealed partial class MasViewModel : ViewModelBase
{
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="MasViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public MasViewModel(Traductor traductor, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _sesion = sesion;
        _navegacion = navegacion;
    }

    /// <summary>Obtiene las opciones permitidas al usuario.</summary>
    /// <value>Opciones del menú.</value>
    public ObservableCollection<OpcionDeMenu> Opciones { get; } = [];

    /// <summary>Arma las opciones según los permisos.</summary>
    [RelayCommand]
    public void Cargar()
    {
        Titulo = T["movil.mas"];
        var opciones = new List<OpcionDeMenu>();

        if (_sesion.Puede(AccionDelSistema.AdministrarEmpleados))
        {
            opciones.Add(new OpcionDeMenu(T["nav.empleados"], T["empleados.subtitulo"], AppShell.RutaEmpleados));
        }

        if (_sesion.Puede(AccionDelSistema.ConsultarExplicacionDeCalculos))
        {
            opciones.Add(new OpcionDeMenu(T["nav.explicacion"], T["explicacion.subtitulo"], AppShell.RutaExplicacion));
        }

        if (_sesion.Puede(AccionDelSistema.AdministrarUsuarios) || _sesion.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo))
        {
            opciones.Add(new OpcionDeMenu(T["movil.administracion"], T["movil.administracionDetalle"], AppShell.RutaAdministracion));
        }

        opciones.Add(new OpcionDeMenu(T["nav.perfil"], T["perfil.subtitulo"], AppShell.RutaCuenta));
        Reemplazar(Opciones, opciones);
    }

    /// <summary>
    /// Abre una opción.
    /// </summary>
    /// <param name="opcion">Opción elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirAsync(OpcionDeMenu? opcion) => opcion is null ? Task.CompletedTask : _navegacion.IrAAsync(opcion.Ruta);

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => Cargar();
}

/// <summary>
/// Cuenta: datos, acciones permitidas, idioma, contraseña y cierre de sesión.
/// </summary>
public sealed partial class CuentaViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly IProveedorDeToken _token;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private bool _sincronizando;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CuentaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="token">Proveedor de token.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public CuentaViewModel(
        Traductor traductor, IServicioDeApi api, IProveedorDeToken token, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _token = token;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["perfil.titulo"];
    }

    /// <summary>Obtiene o establece la identidad del usuario.</summary>
    /// <value><c>null</c> hasta cargar.</value>
    [ObservableProperty]
    public partial UsuarioActualDto? Usuario { get; set; }

    /// <summary>Obtiene las acciones permitidas, traducidas.</summary>
    /// <value>Una por renglón.</value>
    public ObservableCollection<string> Acciones { get; } = [];

    /// <summary>Obtiene los idiomas disponibles.</summary>
    /// <value>Español e inglés.</value>
    public IReadOnlyList<Idioma> Idiomas { get; } = [Idioma.Espanol, Idioma.Ingles];

    /// <summary>Obtiene los nombres traducidos de los idiomas.</summary>
    /// <value>En el mismo orden que <see cref="Idiomas"/>.</value>
    public IReadOnlyList<string> NombresDeIdiomas => [.. Idiomas.Select(i => T.Enumerado(i))];

    /// <summary>Obtiene o establece la posición del idioma elegido.</summary>
    /// <value>0 español, 1 inglés.</value>
    public int IndiceDeIdioma
    {
        get => IdiomaSeleccionado == Idioma.Ingles ? 1 : 0;
        set
        {
            if (value is 0 or 1)
            {
                IdiomaSeleccionado = Idiomas[value];
            }
        }
    }

    /// <summary>Obtiene o establece el idioma elegido.</summary>
    /// <value>Se guarda en el perfil al cambiarlo.</value>
    [ObservableProperty]
    public partial Idioma IdiomaSeleccionado { get; set; }

    /// <summary>Indica si la cuenta tiene contraseña local.</summary>
    /// <value><c>true</c> para mostrar el cambio de contraseña.</value>
    public bool EsLocal => _sesion.EsLocal;

    /// <summary>Indica si el usuario debe cambiar la contraseña asignada.</summary>
    /// <value><c>true</c> tras un alta o restablecimiento.</value>
    public bool RequiereCambio => Usuario?.RequiereCambioDeContrasena == true;

    /// <summary>Obtiene o establece la contraseña actual.</summary>
    /// <value>Sólo en memoria.</value>
    [ObservableProperty]
    public partial string? ContrasenaActual { get; set; }

    /// <summary>Obtiene o establece la contraseña nueva.</summary>
    /// <value>Sólo en memoria.</value>
    [ObservableProperty]
    public partial string? ContrasenaNueva { get; set; }

    /// <summary>Obtiene o establece la confirmación de la contraseña nueva.</summary>
    /// <value>Sólo en memoria.</value>
    [ObservableProperty]
    public partial string? ContrasenaConfirmacion { get; set; }

    /// <summary>
    /// Carga la identidad actual.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        UsuarioActualDto usuario = await _api.ObtenerUsuarioActualAsync();
        _sesion.ActualizarUsuario(usuario);
        Mostrar(usuario);
    });

    /// <summary>
    /// Guarda el idioma en el perfil y lo aplica a la interfaz.
    /// </summary>
    /// <param name="idioma">Idioma elegido.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CambiarIdiomaAsync(Idioma idioma) => EjecutarAsync(async () =>
    {
        await _api.ActualizarPreferenciasAsync(new ActualizarPreferenciasRequest(idioma));
        Textos.Aplicar(idioma);

        if (Usuario is { } usuario)
        {
            Mostrar(usuario with { Idioma = idioma });
            _sesion.ActualizarUsuario(Usuario!);
        }

        MensajeDeExito = T["movil.idiomaGuardado"];
    });

    /// <summary>
    /// Cambia la contraseña local.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CambiarContrasenaAsync() => EjecutarAsync(async () =>
    {
        MensajeDeExito = null;

        if (!string.Equals(ContrasenaNueva, ContrasenaConfirmacion, StringComparison.Ordinal))
        {
            MensajeDeError = T["perfil.contrasenasNoCoinciden"];
            return;
        }

        await _api.CambiarContrasenaAsync(new CambiarContrasenaRequest(ContrasenaActual ?? string.Empty, ContrasenaNueva ?? string.Empty));

        ContrasenaActual = null;
        ContrasenaNueva = null;
        ContrasenaConfirmacion = null;
        MensajeDeExito = T["perfil.contrasenaCambiada"];

        UsuarioActualDto usuario = await _api.ObtenerUsuarioActualAsync();
        _sesion.ActualizarUsuario(usuario);
        Mostrar(usuario);
    });

    /// <summary>
    /// Cierra la sesión y borra el token del dispositivo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CerrarSesionAsync() => EjecutarAsync(async () =>
    {
        await _token.CerrarSesionAsync();
        _sesion.Cerrar();
        Usuario = null;
        await _navegacion.IrAAsync(AppShell.RutaSesion);
    });

    /// <inheritdoc/>
    protected override void AlCambiarIdioma()
    {
        Titulo = T["perfil.titulo"];
        OnPropertyChanged(nameof(NombresDeIdiomas));

        if (Usuario is { } usuario)
        {
            Mostrar(usuario);
        }
    }

    private void Mostrar(UsuarioActualDto usuario)
    {
        _sincronizando = true;
        Usuario = usuario;
        IdiomaSeleccionado = usuario.Idioma;
        _sincronizando = false;

        Reemplazar(Acciones, usuario.Acciones.Select(a => T.Enumerado(a)));
        OnPropertyChanged(nameof(RequiereCambio));
        OnPropertyChanged(nameof(EsLocal));
    }

    partial void OnIdiomaSeleccionadoChanged(Idioma value)
    {
        OnPropertyChanged(nameof(IndiceDeIdioma));

        if (!_sincronizando && Usuario is not null && value != Usuario.Idioma)
        {
            CambiarIdiomaCommand.Execute(value);
        }
    }
}
