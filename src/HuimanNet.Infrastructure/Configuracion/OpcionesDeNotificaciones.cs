namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Opciones del canal de avisos.
/// </summary>
/// <remarks>
/// La petición web sólo encola; el envío de correo lo hace un servicio en
/// segundo plano. Así un fallo del proveedor de correo nunca hace fracasar una
/// carga de documentos (ARQUITECTURA.md §4.1).
/// </remarks>
public sealed class OpcionesDeNotificaciones
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Notificaciones";

    /// <summary>
    /// Obtiene o establece el punto de conexión del servicio de colas.
    /// </summary>
    /// <value>Por ejemplo <c>https://sthuimannetprod.queue.core.windows.net</c>.</value>
    public string UriServicioColas { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la cadena de conexión con clave de cuenta.
    /// </summary>
    /// <value>Alternativa exclusiva para desarrollo local con Azurite.</value>
    public string CadenaDeConexion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el nombre de la cola de avisos.
    /// </summary>
    /// <value><c>avisos</c> por defecto.</value>
    public string NombreDeCola { get; set; } = "avisos";

    /// <summary>
    /// Obtiene o establece si el arranque debe crear la cola si no existe.
    /// </summary>
    /// <value><c>true</c> en desarrollo. En Azure la crea la plantilla de Bicep.</value>
    public bool CrearColaAlIniciar { get; set; }

    /// <summary>
    /// Obtiene o establece si el canal de avisos está habilitado.
    /// </summary>
    /// <value>
    /// Cuando es <c>false</c>, los avisos se registran en el log y se descartan.
    /// Útil en desarrollo y en pruebas de integración.
    /// </value>
    public bool Habilitado { get; set; } = true;
}
