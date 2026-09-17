using System.Data;
using HuimanNet.Infrastructure.Persistence.Connections;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base común de los repositorios ADO.NET: crea comandos ya enlazados a la
/// conexión y a la transacción de la petición en curso.
/// </summary>
/// <remarks>
/// Compatible con Native AOT: no usa reflexión, emisión de IL ni convenciones
/// de mapeo automáticas. Todos los parámetros van tipados y el SQL vive en
/// procedimientos almacenados (ESPECIFICACION.md §6.1).
/// </remarks>
public abstract class RepositorioSqlBase
{
    private readonly ISesionSql _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RepositorioSqlBase"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="sesion"/> es <c>null</c>.
    /// </exception>
    protected RepositorioSqlBase(ISesionSql sesion)
    {
        ArgumentNullException.ThrowIfNull(sesion);
        _sesion = sesion;
    }

    /// <summary>
    /// Obtiene la sesión de base de datos de la petición en curso.
    /// </summary>
    /// <value>Da acceso a la conexión y a la transacción para operaciones masivas.</value>
    protected ISesionSql Sesion => _sesion;

    /// <summary>
    /// Crea un comando que invoca un procedimiento almacenado, enlazado a la
    /// conexión y a la transacción de la petición.
    /// </summary>
    /// <param name="nombre">
    /// Nombre completo del procedimiento, tomado de <see cref="Procedimientos"/>.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El comando listo para añadirle parámetros y ejecutarse.</returns>
    /// <remarks>
    /// Es la única vía de acceso a datos de la solución: ningún repositorio
    /// arma SQL por su cuenta. El plan de ejecución lo reutiliza el servidor y
    /// la base de datos puede conceder sólo permiso de <c>EXECUTE</c>.
    /// </remarks>
    protected async Task<SqlCommand> CrearProcedimientoAsync(
        string nombre, CancellationToken cancellationToken)
    {
        SqlConnection conexion = await _sesion.ObtenerConexionAsync(cancellationToken);

        SqlCommand comando = conexion.CreateCommand();
        comando.CommandType = CommandType.StoredProcedure;
        comando.CommandText = nombre;
        comando.CommandTimeout = _sesion.TiempoDeEsperaComandoSegundos;
        comando.Transaction = _sesion.TransaccionActual;

        return comando;
    }

}
