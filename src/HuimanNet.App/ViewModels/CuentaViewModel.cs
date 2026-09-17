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

    /// <summary>
    /// Muestra los datos del usuario sin disparar el cambio de idioma que
    /// provocaría fijar <see cref="IdiomaSeleccionado"/>.
    /// </summary>
    /// <param name="usuario">Identidad a mostrar.</param>
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

    /// <summary>Guarda el idioma cuando lo cambia el usuario.</summary>
    /// <param name="value">Idioma elegido.</param>
    partial void OnIdiomaSeleccionadoChanged(Idioma value)
    {
        OnPropertyChanged(nameof(IndiceDeIdioma));

        if (!_sincronizando && Usuario is not null && value != Usuario.Idioma)
        {
            CambiarIdiomaCommand.Execute(value);
        }
    }
}
