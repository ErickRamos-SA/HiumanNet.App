namespace HuimanNet.Web.Notificaciones;

/// <summary>
/// Opciones del envío de correo con Azure Communication Services.
/// </summary>
public sealed class OpcionesDeCorreo
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Correo";

    /// <summary>
    /// Obtiene o establece el punto de conexión del recurso de comunicaciones.
    /// </summary>
    /// <value>
    /// Por ejemplo <c>https://acs-huimannet.communication.azure.com</c>. Cuando
    /// tiene valor se usa identidad administrada.
    /// </value>
    public string UriServicio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la cadena de conexión del recurso de comunicaciones.
    /// </summary>
    /// <value>Alternativa para entornos sin identidad administrada.</value>
    public string CadenaDeConexion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección remitente verificada.
    /// </summary>
    /// <value>Por ejemplo <c>no-reply@huimannet.com</c>.</value>
    public string Remitente { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección pública del portal.
    /// </summary>
    /// <value>Se incluye en el cuerpo del aviso para que el destinatario entre.</value>
    public string UrlDelPortal { get; set; } = "https://huimannet.example";

    /// <summary>
    /// Obtiene o establece si el trabajador de avisos debe ejecutarse.
    /// </summary>
    /// <value><c>false</c> en desarrollo, para no depender de una cuenta de correo.</value>
    public bool Habilitado { get; set; }

    /// <summary>
    /// Obtiene o establece la pausa entre sondeos de la cola cuando está vacía.
    /// </summary>
    /// <value>30 segundos por defecto.</value>
    public TimeSpan IntervaloDeSondeo { get; set; } = TimeSpan.FromSeconds(30);
}
