using System.Globalization;

namespace HuimanNet.Contracts;

/// <summary>
/// Rutas de la API HTTP, compartidas entre el servidor y los clientes para que
/// no puedan divergir.
/// </summary>
/// <remarks>
/// La API está versionada: cualquier cambio incompatible estrena
/// <c>/api/v2</c> en lugar de modificar estas rutas (ESPECIFICACION.md §9).
/// </remarks>
public static class RutasApi
{
    /// <summary>Prefijo de versión de la API.</summary>
    public const string Base = "/api/v1";

    /// <summary>Ruta del endpoint de sondeo de vida del servicio.</summary>
    public const string Salud = "/salud";

    /// <summary>Configuración pública del servidor (modo de identidad, versión).</summary>
    public const string Configuracion = Base + "/configuracion";

    /// <summary>Inicio de sesión con credenciales locales.</summary>
    public const string IniciarSesion = Base + "/sesion/iniciar";

    /// <summary>Ruta que devuelve la identidad efectiva del solicitante.</summary>
    public const string UsuarioActual = Base + "/mi-usuario";

    /// <summary>Cambio de contraseña del propio usuario.</summary>
    public const string CambiarContrasena = UsuarioActual + "/contrasena";

    /// <summary>Preferencias del propio usuario (idioma).</summary>
    public const string Preferencias = UsuarioActual + "/preferencias";

    /// <summary>Indicadores del panel de inicio.</summary>
    public const string Inicio = Base + "/inicio";

    /// <summary>Colección de empresas.</summary>
    public const string Empresas = Base + "/empresas";

    /// <summary>Colección de razones sociales.</summary>
    public const string RazonesSociales = Base + "/razones-sociales";

    /// <summary>Colección de empleados.</summary>
    public const string Empleados = Base + "/empleados";

    /// <summary>Colección de contratos.</summary>
    public const string Contratos = Base + "/contratos";

    /// <summary>Colección de períodos de carga.</summary>
    public const string Periodos = Base + "/periodos";

    /// <summary>Bandeja transversal del operador de nómina.</summary>
    public const string BandejaOperador = Periodos + "/bandeja";

    /// <summary>Colección de documentos.</summary>
    public const string Documentos = Base + "/documentos";

    /// <summary>Punto de entrada de los veredictos de escaneo de malware.</summary>
    public const string ResultadoEscaneo = Documentos + "/resultado-escaneo";

    /// <summary>Colección de incidencias.</summary>
    public const string Incidencias = Base + "/incidencias";

    /// <summary>Importación de incidencias desde archivo.</summary>
    public const string ImportarIncidencias = Incidencias + "/importar";

    /// <summary>Colección de corridas de nómina.</summary>
    public const string Nomina = Base + "/nomina";

    /// <summary>Ejecución del cálculo de nómina.</summary>
    public const string CalcularNomina = Nomina + "/calcular";

    /// <summary>Cotejo de una corrida.</summary>
    public const string CotejarNomina = Nomina + "/cotejar";

    /// <summary>Catálogo de parámetros de cálculo.</summary>
    public const string Parametros = Base + "/catalogos/parametros";

    /// <summary>Catálogo de tablas por rangos.</summary>
    public const string Tablas = Base + "/catalogos/tablas";

    /// <summary>Catálogo de conceptos de nómina.</summary>
    public const string Conceptos = Base + "/catalogos/conceptos";

    /// <summary>Prueba de fórmulas.</summary>
    public const string ProbarFormula = Conceptos + "/probar";

    /// <summary>Secciones de explicación de cálculos.</summary>
    public const string Explicaciones = Base + "/catalogos/explicaciones";

    /// <summary>Explicación completa de un esquema.</summary>
    public const string ExplicacionCompleta = Explicaciones + "/completa";

    /// <summary>Colección de usuarios (administración).</summary>
    public const string Usuarios = Base + "/usuarios";

    /// <summary>Bitácora de auditoría.</summary>
    public const string Auditoria = Base + "/auditoria";

    /// <summary>Punto de entrada del almacén local de documentos (sólo desarrollo).</summary>
    public const string AlmacenLocal = "/almacen-local";

    /// <summary>
    /// Construye la ruta de un recurso por identificador.
    /// </summary>
    /// <param name="coleccion">Ruta de la colección.</param>
    /// <param name="id">Identificador del recurso.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string Recurso(string coleccion, Guid id)
        => string.Create(CultureInfo.InvariantCulture, $"{coleccion}/{id}");

    /// <summary>
    /// Construye la ruta de los documentos de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string DocumentosDePeriodo(Guid periodoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Periodos}/{periodoId}/documentos");

    /// <summary>
    /// Construye la ruta de las incidencias de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string IncidenciasDePeriodo(Guid periodoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Periodos}/{periodoId}/incidencias");

    /// <summary>
    /// Construye la ruta de las corridas de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string CorridasDePeriodo(Guid periodoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Periodos}/{periodoId}/nomina");

    /// <summary>
    /// Construye la ruta para confirmar la carga de un documento.
    /// </summary>
    /// <param name="documentoId">Documento cuya carga se confirma.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string ConfirmarCarga(Guid documentoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Documentos}/{documentoId}/confirmar");

    /// <summary>
    /// Construye la ruta para obtener el enlace de descarga de un documento.
    /// </summary>
    /// <param name="documentoId">Documento a descargar.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string EnlaceDescarga(Guid documentoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Documentos}/{documentoId}/enlace-descarga");

    /// <summary>
    /// Construye la ruta para cambiar el estado de un período.
    /// </summary>
    /// <param name="periodoId">Período afectado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string EstadoDePeriodo(Guid periodoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Periodos}/{periodoId}/estado");

    /// <summary>
    /// Construye la ruta del resumen de una corrida (resultados y facturación).
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string ResumenDeCorrida(Guid corridaId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/{corridaId}/resumen");

    /// <summary>
    /// Construye la ruta del detalle de un resultado de nómina.
    /// </summary>
    /// <param name="resultadoId">Resultado consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string DetalleDeResultado(Guid resultadoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/resultados/{resultadoId}");

    /// <summary>
    /// Construye la ruta de exportación CSV de una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string ExportarCorrida(Guid corridaId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/{corridaId}/exportar");

    /// <summary>
    /// Construye la ruta para cambiar el estado de una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida afectada.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string EstadoDeCorrida(Guid corridaId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/{corridaId}/estado");

    /// <summary>
    /// Construye la ruta de los cotejos de una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string CotejosDeCorrida(Guid corridaId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/{corridaId}/cotejos");

    /// <summary>
    /// Construye la ruta de un cotejo con su detalle.
    /// </summary>
    /// <param name="cotejoId">Cotejo consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string Cotejo(Guid cotejoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Nomina}/cotejos/{cotejoId}");

    /// <summary>
    /// Construye la ruta de los contratos de un empleado.
    /// </summary>
    /// <param name="empleadoId">Empleado consultado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string ContratosDeEmpleado(Guid empleadoId)
        => string.Create(CultureInfo.InvariantCulture, $"{Empleados}/{empleadoId}/contratos");

    /// <summary>
    /// Construye la ruta para restablecer la contraseña de un usuario.
    /// </summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <returns>Ruta relativa del recurso.</returns>
    public static string RestablecerContrasena(Guid usuarioId)
        => string.Create(CultureInfo.InvariantCulture, $"{Usuarios}/{usuarioId}/contrasena");
}
