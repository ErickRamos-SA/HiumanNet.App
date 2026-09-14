namespace HuimanNet.Api.Configuracion;

/// <summary>
/// Opciones de validación del token de Microsoft Entra.
/// </summary>
/// <remarks>
/// La API es <b>stateless</b>: no mantiene sesión, sólo valida el JWT en cada
/// petición. Eso es lo que permite escalar horizontalmente sin sesión pegajosa
/// (ARQUITECTURA.md §2).
/// </remarks>
public sealed class OpcionesDeEntra
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Entra";

    /// <summary>
    /// Obtiene o establece la autoridad emisora del token.
    /// </summary>
    /// <value>Por ejemplo <c>https://login.microsoftonline.com/{tenantId}/v2.0</c>.</value>
    public string Autoridad { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el identificador de audiencia que debe traer el token.
    /// </summary>
    /// <value>Identificador de la aplicación registrada para la API.</value>
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el margen de tolerancia de reloj al validar el token.
    /// </summary>
    /// <value>Dos minutos por defecto.</value>
    public TimeSpan ToleranciaDeReloj { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Obtiene o establece si debe exigirse HTTPS en los metadatos del emisor.
    /// </summary>
    /// <value><c>true</c> salvo en desarrollo con un emisor local.</value>
    public bool ExigirHttpsEnMetadatos { get; set; } = true;
}
