namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Configuración de identidad: modo de autenticación, nombres de <i>claims</i>
/// y roles de Microsoft Entra, y parámetros del modo local.
/// </summary>
/// <remarks>
/// Viven en infraestructura —y no en cada anfitrión— para que la API y la web
/// Blazor traduzcan la identidad exactamente igual. Si divergieran, un mismo
/// token daría permisos distintos según el canal.
/// <para>
/// <b>Modo Local</b>: usuarios y contraseñas gestionados por el administrador
/// del portal (desarrollo o instalaciones sin Entra). <b>Modo Entra</b>: la
/// autenticación la hace Microsoft Entra; el rol, la empresa y los permisos
/// siguen gestionándose en el portal.
/// </para>
/// </remarks>
public sealed class OpcionesDeIdentidad
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Identidad";

    /// <summary>Valor de <see cref="Modo"/> para autenticación con Microsoft Entra.</summary>
    public const string ModoEntra = "Entra";

    /// <summary>Valor de <see cref="Modo"/> para autenticación con usuarios locales.</summary>
    public const string ModoLocal = "Local";

    /// <summary>
    /// Obtiene o establece el modo de autenticación.
    /// </summary>
    /// <value><c>Entra</c> (predeterminado) o <c>Local</c>.</value>
    public string Modo { get; set; } = ModoEntra;

    /// <summary>
    /// Indica si el portal opera con usuarios locales.
    /// </summary>
    /// <value><c>true</c> cuando <see cref="Modo"/> es <c>Local</c>.</value>
    public bool EsLocal => string.Equals(Modo, ModoLocal, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene o establece el <i>claim</i> que porta la empresa del usuario en tokens de Entra.
    /// </summary>
    /// <value><c>extension_EmpresaId</c> por defecto.</value>
    public string ClaimDeEmpresa { get; set; } = "extension_EmpresaId";

    /// <summary>Obtiene o establece el nombre del rol de aplicación de empresa cliente.</summary>
    /// <value><c>ClienteEmpresa</c> por defecto.</value>
    public string RolClienteEmpresa { get; set; } = "ClienteEmpresa";

    /// <summary>Obtiene o establece el nombre del rol de aplicación del operador de nómina.</summary>
    /// <value><c>OperadorNomina</c> por defecto.</value>
    public string RolOperadorNomina { get; set; } = "OperadorNomina";

    /// <summary>Obtiene o establece el nombre del rol de aplicación del administrador.</summary>
    /// <value><c>Administrador</c> por defecto.</value>
    public string RolAdministrador { get; set; } = "Administrador";

    /// <summary>
    /// Obtiene o establece la clave con la que se firman los tokens locales.
    /// </summary>
    /// <value>
    /// Texto de al menos 32 caracteres, resuelto desde Key Vault o secretos de
    /// usuario. Vacío en desarrollo: se genera una clave aleatoria por proceso.
    /// </value>
    public string ClaveDeFirmaLocal { get; set; } = string.Empty;

    /// <summary>Obtiene o establece el emisor de los tokens locales.</summary>
    /// <value><c>huimannet</c> por defecto.</value>
    public string EmisorLocal { get; set; } = "huimannet";

    /// <summary>Obtiene o establece la audiencia de los tokens locales.</summary>
    /// <value><c>huimannet-api</c> por defecto.</value>
    public string AudienciaLocal { get; set; } = "huimannet-api";

    /// <summary>Obtiene o establece la vigencia de los tokens locales, en minutos.</summary>
    /// <value>480 minutos (una jornada) por defecto.</value>
    public int MinutosVigenciaTokenLocal { get; set; } = 480;

    /// <summary>Obtiene o establece el número de iteraciones de PBKDF2 para las contraseñas.</summary>
    /// <value>210 000 por defecto (recomendación OWASP para PBKDF2-SHA512).</value>
    public int IteracionesDeHash { get; set; } = 210_000;

    /// <summary>Obtiene o establece el administrador que se crea si no existe ninguno.</summary>
    /// <value>Vacío en producción con Entra; con valores en desarrollo.</value>
    public OpcionesDeAdministradorInicial AdministradorInicial { get; set; } = new();
}

/// <summary>
/// Administrador que se crea al arrancar cuando la base de datos no tiene ninguno.
/// </summary>
/// <remarks>
/// Sólo existe para poder entrar la primera vez. El usuario queda obligado a
/// cambiar la contraseña en su primer acceso.
/// </remarks>
public sealed class OpcionesDeAdministradorInicial
{
    /// <summary>Obtiene o establece el correo del administrador inicial.</summary>
    /// <value>Vacío para no crear ninguno.</value>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Obtiene o establece el nombre del administrador inicial.</summary>
    /// <value><c>Administrador</c> por defecto.</value>
    public string NombreCompleto { get; set; } = "Administrador";

    /// <summary>Obtiene o establece la contraseña inicial.</summary>
    /// <value>Debe resolverse desde secretos; nunca en <c>appsettings.json</c> de producción.</value>
    public string Contrasena { get; set; } = string.Empty;
}
