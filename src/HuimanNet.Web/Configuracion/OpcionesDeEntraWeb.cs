namespace HuimanNet.Web.Configuracion;

/// <summary>
/// Opciones del inicio de sesión OpenID Connect contra Microsoft Entra.
/// </summary>
/// <remarks>
/// Se declara como POCO propio en lugar de enlazar directamente
/// <c>OpenIdConnectOptions</c>: ese tipo contiene propiedades que el generador
/// de enlace de configuración no sabe construir (manejadores HTTP, validadores
/// de token), y enlazarlo produciría advertencias SYSLIB1100/SYSLIB1101.
/// </remarks>
public sealed class OpcionesDeEntraWeb
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Entra";

    /// <summary>
    /// Obtiene o establece la autoridad emisora.
    /// </summary>
    /// <value>Por ejemplo <c>https://login.microsoftonline.com/{tenantId}/v2.0</c>.</value>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el identificador de la aplicación registrada.
    /// </summary>
    /// <value>Identificador de cliente de la aplicación web en Entra.</value>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el secreto de cliente.
    /// </summary>
    /// <value>
    /// Debe resolverse desde Key Vault o desde los secretos de usuario en
    /// desarrollo. Nunca debe quedar escrito en <c>appsettings.json</c>.
    /// </value>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la ruta de retorno tras el inicio de sesión.
    /// </summary>
    /// <value><c>/signin-oidc</c> por defecto.</value>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Obtiene o establece la ruta de retorno tras el cierre de sesión.
    /// </summary>
    /// <value><c>/signout-callback-oidc</c> por defecto.</value>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>
    /// Obtiene o establece si se exige HTTPS al descargar los metadatos del emisor.
    /// </summary>
    /// <value><c>true</c> salvo en desarrollo contra un emisor local.</value>
    public bool RequireHttpsMetadata { get; set; } = true;
}
