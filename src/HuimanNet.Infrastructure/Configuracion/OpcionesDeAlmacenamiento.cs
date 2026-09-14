namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Opciones del almacenamiento de documentos.
/// </summary>
/// <remarks>
/// El proveedor es intercambiable por configuración: <c>Local</c> guarda los
/// archivos en el disco del servidor (desarrollo) y <c>AzureBlob</c> en Azure
/// Blob Storage (despliegue). Ambos implementan el mismo contrato
/// <see cref="Application.Interfaces.IAlmacenDocumentos"/> con enlaces firmados
/// de corta vigencia, así que migrar de uno a otro no toca ninguna capa superior:
/// basta con cambiar <see cref="Proveedor"/> y <see cref="UriServicio"/>.
/// </remarks>
public sealed class OpcionesDeAlmacenamiento
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Almacenamiento";

    /// <summary>Valor de <see cref="Proveedor"/> para almacenamiento en disco local.</summary>
    public const string ProveedorLocal = "Local";

    /// <summary>Valor de <see cref="Proveedor"/> para Azure Blob Storage.</summary>
    public const string ProveedorAzureBlob = "AzureBlob";

    /// <summary>
    /// Obtiene o establece el proveedor de almacenamiento.
    /// </summary>
    /// <value><c>AzureBlob</c> (predeterminado) o <c>Local</c>.</value>
    public string Proveedor { get; set; } = ProveedorAzureBlob;

    /// <summary>
    /// Indica si el almacenamiento es el disco local.
    /// </summary>
    /// <value><c>true</c> cuando <see cref="Proveedor"/> es <c>Local</c>.</value>
    public bool EsLocal => string.Equals(Proveedor, ProveedorLocal, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene o establece el punto de conexión del servicio Blob.
    /// </summary>
    /// <value>
    /// Por ejemplo <c>https://sthuimannetprod.blob.core.windows.net</c>. Cuando
    /// tiene valor, la autenticación se hace con identidad administrada y las
    /// firmas se generan con una <i>user delegation key</i>.
    /// </value>
    public string UriServicio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la cadena de conexión con clave de cuenta.
    /// </summary>
    /// <value>Alternativa para Azurite o cuentas sin identidad administrada; <see cref="UriServicio"/> tiene prioridad.</value>
    public string CadenaDeConexion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el contenedor (o carpeta, en local) donde viven los documentos.
    /// </summary>
    /// <value><c>documentos</c> por defecto. Nunca es público.</value>
    public string ContenedorDocumentos { get; set; } = "documentos";

    /// <summary>
    /// Obtiene o establece el contenedor (o carpeta) de cuarentena.
    /// </summary>
    /// <value>Destino de los archivos que el antimalware marca como maliciosos.</value>
    public string ContenedorCuarentena { get; set; } = "cuarentena";

    /// <summary>
    /// Obtiene o establece la vigencia de las firmas de escritura, en minutos.
    /// </summary>
    /// <value>15 minutos por defecto: suficiente para subir un archivo grande, no más.</value>
    public int MinutosVigenciaEscritura { get; set; } = 15;

    /// <summary>
    /// Obtiene o establece la vigencia de las firmas de lectura, en minutos.
    /// </summary>
    /// <value>5 minutos por defecto: la descarga empieza de inmediato tras pedir el enlace.</value>
    public int MinutosVigenciaLectura { get; set; } = 5;

    /// <summary>
    /// Obtiene o establece el margen de tolerancia de reloj de las firmas, en minutos.
    /// </summary>
    /// <value>5 minutos por defecto.</value>
    public int MinutosToleranciaDeReloj { get; set; } = 5;

    /// <summary>
    /// Obtiene o establece si el arranque debe crear los contenedores que falten.
    /// </summary>
    /// <value><c>true</c> en desarrollo. En Azure los crea la plantilla de Bicep.</value>
    public bool CrearContenedoresAlIniciar { get; set; }

    /// <summary>
    /// Obtiene o establece la carpeta raíz del almacenamiento local.
    /// </summary>
    /// <value>
    /// Admite variables de entorno (<c>%LOCALAPPDATA%</c>). Vacío para usar
    /// <c>%LOCALAPPDATA%/HuimanNet/almacen</c>, compartida por la web y la API
    /// de la misma máquina.
    /// </value>
    public string RutaLocal { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección pública del anfitrión que sirve los enlaces locales.
    /// </summary>
    /// <value>Vacío para emitir enlaces relativos al propio anfitrión (lo habitual).</value>
    public string UrlPublicaLocal { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la clave con la que se firman los enlaces locales.
    /// </summary>
    /// <value>Vacío para generar una clave aleatoria por proceso.</value>
    public string ClaveDeFirmaLocal { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece si en almacenamiento local el documento se declara
    /// limpio en cuanto se confirma la carga.
    /// </summary>
    /// <value>
    /// <c>true</c> por defecto: en desarrollo no hay antimalware. En Azure el
    /// veredicto lo publica Defender for Storage y este valor se ignora.
    /// </value>
    public bool EscaneoInmediatoLocal { get; set; } = true;
}
