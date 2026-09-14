namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Opciones de conexión a SQL Server.
/// </summary>
/// <remarks>
/// En Azure la cadena no lleva credenciales: se usa
/// <c>Authentication=Active Directory Default</c> con la identidad administrada
/// de la aplicación, de modo que no hay contraseñas que rotar ni que filtrar
/// (ARQUITECTURA.md §6.4).
/// </remarks>
public sealed class OpcionesDeSqlServer
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "SqlServer";

    /// <summary>
    /// Obtiene o establece la cadena de conexión a la base de datos.
    /// </summary>
    /// <value>Nunca debe contener secretos en producción; se resuelve vía Key Vault o identidad administrada.</value>
    public string CadenaDeConexion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el tiempo máximo de ejecución de cada comando, en segundos.
    /// </summary>
    /// <value>30 segundos por defecto.</value>
    public int TiempoDeEsperaComandoSegundos { get; set; } = 30;

    /// <summary>
    /// Obtiene o establece si el arranque debe aplicar los scripts de esquema pendientes.
    /// </summary>
    /// <value>
    /// <c>true</c> en desarrollo y pruebas. En producción conviene dejarlo en
    /// <c>false</c> y aplicar los scripts desde el flujo de CI/CD.
    /// </value>
    public bool AplicarScriptsAlIniciar { get; set; }

    /// <summary>
    /// Obtiene o establece si el arranque debe crear la base de datos cuando no existe.
    /// </summary>
    /// <value>
    /// <c>true</c> sólo en desarrollo con SQL Server local (Express o Developer):
    /// se conecta a <c>master</c> con las mismas credenciales y ejecuta
    /// <c>CREATE DATABASE</c>. En Azure SQL la base de datos la aprovisiona la
    /// plantilla de infraestructura y este valor debe quedar en <c>false</c>.
    /// </value>
    public bool CrearBaseDeDatosSiFalta { get; set; }

    /// <summary>
    /// Obtiene o establece si el arranque debe cargar los datos iniciales que falten.
    /// </summary>
    /// <value>
    /// <c>true</c> por defecto. Carga el catálogo de cálculo inicial cuando está
    /// vacío y crea el administrador inicial cuando no hay ninguno. Es seguro en
    /// cualquier entorno, Azure SQL incluido: nunca sobrescribe datos existentes.
    /// </value>
    public bool SembrarDatosIniciales { get; set; } = true;

    /// <summary>
    /// Obtiene o establece si el arranque debe cargar las empresas, empleados,
    /// períodos y usuarios de prueba.
    /// </summary>
    /// <value>
    /// <c>false</c> por defecto; <c>true</c> sólo en <c>appsettings.Development.json</c>.
    /// Nunca debe habilitarse en Azure: crea usuarios con la contraseña del
    /// administrador. Consulte <see cref="Persistence.Semillas.SembradorDeDatosDePrueba"/>.
    /// </value>
    public bool SembrarDatosDePrueba { get; set; }
}
