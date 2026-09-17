using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

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
    /// <summary>Clave de las preferencias donde se guarda el modo de identidad.</summary>
    private const string ClaveModo = "huimannet.modo";

    /// <summary>Clave de las preferencias donde se guarda la última empresa elegida.</summary>
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
