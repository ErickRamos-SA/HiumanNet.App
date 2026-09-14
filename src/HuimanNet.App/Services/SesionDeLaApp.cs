using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

/// <summary>Aviso de que el usuario inició sesión y su identidad ya se conoce.</summary>
/// <param name="Usuario">Identidad efectiva devuelta por la API.</param>
public sealed record SesionIniciadaMensaje(UsuarioActualDto Usuario);

/// <summary>Aviso de que cambió la empresa de trabajo.</summary>
/// <param name="EmpresaId">Empresa elegida, o <c>null</c>.</param>
public sealed record EmpresaCambiadaMensaje(Guid? EmpresaId);

/// <summary>
/// Estado de la sesión en el dispositivo: modo de identidad, usuario, permisos
/// y empresa de trabajo.
/// </summary>
/// <remarks>
/// Es el equivalente móvil de <c>ContextoDeUsuarioDelCircuito</c> y
/// <c>EstadoDelPortal</c> de la web. Los permisos sólo gobiernan qué se
/// muestra: la API vuelve a autorizar cada operación.
/// </remarks>
public sealed class SesionDeLaApp
{
    private const string ClaveModo = "huimannet.modo";
    private const string ClaveEmpresa = "huimannet.empresa";

    /// <summary>Obtiene o establece el modo de identidad que usa el servidor.</summary>
    /// <value><c>Entra</c> o <c>Local</c>; se recuerda entre ejecuciones.</value>
    public string ModoDeIdentidad
    {
        get => Preferences.Default.Get(ClaveModo, "Entra");
        set => Preferences.Default.Set(ClaveModo, value);
    }

    /// <summary>Indica si el servidor usa cuentas locales con contraseña.</summary>
    /// <value><c>true</c> en modo <c>Local</c>.</value>
    public bool EsLocal => string.Equals(ModoDeIdentidad, "Local", StringComparison.OrdinalIgnoreCase);

    /// <summary>Obtiene la identidad efectiva del usuario.</summary>
    /// <value><c>null</c> mientras no hay sesión.</value>
    public UsuarioActualDto? Usuario { get; private set; }

    /// <summary>Obtiene las empresas que puede elegir el usuario.</summary>
    /// <value>Todas las activas para los roles transversales; las suyas para la empresa cliente.</value>
    public IReadOnlyList<EmpresaDto> Empresas { get; private set; } = [];

    /// <summary>Obtiene la empresa de trabajo.</summary>
    /// <value><c>null</c> si un rol transversal no ha elegido ninguna.</value>
    public EmpresaDto? Empresa { get; private set; }

    /// <summary>Indica si el usuario trabaja sobre todas las empresas.</summary>
    /// <value><c>true</c> para operador de nómina y administrador.</value>
    public bool EsTransversal => Usuario?.EsTransversal == true;

    /// <summary>Indica si el usuario elige la empresa de trabajo.</summary>
    /// <value><c>true</c> para nómina y administración, y para la empresa cliente con más de una empresa.</value>
    public bool EligeEmpresa => EsTransversal || Empresas.Count > 1;

    /// <summary>Obtiene la empresa que se envía a la API en las consultas.</summary>
    /// <value>La empresa de trabajo si el usuario elige empresa; <c>null</c> para la empresa cliente con una sola (la API usa la suya).</value>
    public Guid? EmpresaDeConsulta => EligeEmpresa ? Empresa?.Id : null;

    /// <summary>Indica si el usuario todavía no eligió empresa.</summary>
    /// <value><c>true</c> si falta la empresa de trabajo.</value>
    public bool FaltaEmpresa => EligeEmpresa && Empresa is null;

    /// <summary>
    /// Indica si el usuario puede ejecutar una acción.
    /// </summary>
    /// <param name="accion">Acción consultada.</param>
    /// <returns><c>true</c> si está entre sus acciones efectivas.</returns>
    public bool Puede(AccionDelSistema accion) => Usuario?.Puede(accion) == true;

    /// <summary>
    /// Registra la identidad tras iniciar sesión y restaura la última empresa elegida.
    /// </summary>
    /// <param name="usuario">Identidad efectiva.</param>
    /// <param name="empresas">Empresas disponibles para roles transversales.</param>
    public void Iniciar(UsuarioActualDto usuario, IReadOnlyList<EmpresaDto> empresas)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(empresas);

        Usuario = usuario;
        Empresas = empresas;

        string? guardada = Preferences.Default.Get<string?>(ClaveEmpresa, null);
        Empresa = empresas.FirstOrDefault(e => string.Equals(e.Id.ToString(), guardada, StringComparison.OrdinalIgnoreCase))
            ?? (empresas.Count == 1 ? empresas[0] : null);

        Textos.Aplicar(usuario.Idioma);
        WeakReferenceMessenger.Default.Send(new SesionIniciadaMensaje(usuario));
    }

    /// <summary>
    /// Sustituye la identidad (por ejemplo, tras cambiar la contraseña o el idioma).
    /// </summary>
    /// <param name="usuario">Identidad actualizada.</param>
    public void ActualizarUsuario(UsuarioActualDto usuario) => Usuario = usuario;

    /// <summary>
    /// Cambia la empresa de trabajo y la recuerda en el dispositivo.
    /// </summary>
    /// <param name="empresa">Empresa elegida, o <c>null</c>.</param>
    public void SeleccionarEmpresa(EmpresaDto? empresa)
    {
        if (empresa?.Id == Empresa?.Id)
        {
            return;
        }

        Empresa = empresa;

        if (empresa is null)
        {
            Preferences.Default.Remove(ClaveEmpresa);
        }
        else
        {
            Preferences.Default.Set(ClaveEmpresa, empresa.Id.ToString());
        }

        WeakReferenceMessenger.Default.Send(new EmpresaCambiadaMensaje(empresa?.Id));
    }

    /// <summary>Olvida la identidad al cerrar sesión.</summary>
    public void Cerrar()
    {
        Usuario = null;
        Empresas = [];
        Empresa = null;
    }
}

/// <summary>
/// Token del modo de identidad local, guardado en el almacén seguro del sistema
/// (Keystore en Android, Keychain en iOS).
/// </summary>
public sealed class ProveedorDeTokenLocal
{
    private const string ClaveToken = "huimannet.token";
    private const string ClaveExpiracion = "huimannet.token.expira";

    private string? _token;
    private DateTimeOffset _expira = DateTimeOffset.MinValue;
    private bool _cargado;

    /// <summary>
    /// Obtiene el token vigente.
    /// </summary>
    /// <returns>El token, o <c>null</c> si no hay o está por caducar.</returns>
    public async Task<string?> ObtenerAsync()
    {
        if (!_cargado)
        {
            _token = await SecureStorage.Default.GetAsync(ClaveToken);
            string? expira = await SecureStorage.Default.GetAsync(ClaveExpiracion);
            _expira = long.TryParse(expira, NumberStyles.None, CultureInfo.InvariantCulture, out long segundos)
                ? DateTimeOffset.FromUnixTimeSeconds(segundos)
                : DateTimeOffset.MinValue;
            _cargado = true;
        }

        return _token is not null && _expira > DateTimeOffset.UtcNow.AddMinutes(1) ? _token : null;
    }

    /// <summary>
    /// Guarda el token emitido por la API.
    /// </summary>
    /// <param name="token">Token compacto.</param>
    /// <param name="expira">Instante de caducidad.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EstablecerAsync(string token, DateTimeOffset expira)
    {
        _token = token;
        _expira = expira;
        _cargado = true;

        await SecureStorage.Default.SetAsync(ClaveToken, token);
        await SecureStorage.Default.SetAsync(ClaveExpiracion, expira.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Borra el token del dispositivo.</summary>
    public void Borrar()
    {
        _token = null;
        _expira = DateTimeOffset.MinValue;
        _cargado = true;
        SecureStorage.Default.Remove(ClaveToken);
        SecureStorage.Default.Remove(ClaveExpiracion);
    }
}

/// <summary>
/// <see cref="IProveedorDeToken"/> que delega en Microsoft Entra o en el token
/// local según el modo de identidad del servidor.
/// </summary>
public sealed class ProveedorDeToken : IProveedorDeToken
{
    private readonly SesionDeLaApp _sesion;
    private readonly ProveedorDeTokenLocal _local;
    private readonly IServiceProvider _servicios;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ProveedorDeToken"/>.
    /// </summary>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="local">Token del modo local.</param>
    /// <param name="servicios">Contenedor, para crear el cliente de Entra sólo si hace falta.</param>
    public ProveedorDeToken(SesionDeLaApp sesion, ProveedorDeTokenLocal local, IServiceProvider servicios)
    {
        _sesion = sesion;
        _local = local;
        _servicios = servicios;
    }

    private ProveedorDeTokenEntra Entra => _servicios.GetRequiredService<ProveedorDeTokenEntra>();

    /// <inheritdoc/>
    public Task<string?> ObtenerTokenSilenciosoAsync(CancellationToken cancellationToken = default)
        => _sesion.EsLocal ? _local.ObtenerAsync() : Entra.ObtenerTokenSilenciosoAsync(cancellationToken);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// En modo local, si el token caducó: el usuario debe volver a escribir su contraseña.
    /// </exception>
    public Task<string> IniciarSesionAsync(CancellationToken cancellationToken = default)
    {
        if (_sesion.EsLocal)
        {
            _local.Borrar();
            throw new InvalidOperationException(Textos.Traductor["movil.sesionExpirada"]);
        }

        return Entra.IniciarSesionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CerrarSesionAsync(CancellationToken cancellationToken = default)
    {
        _local.Borrar();

        if (!_sesion.EsLocal)
        {
            await Entra.CerrarSesionAsync(cancellationToken);
        }
    }
}
