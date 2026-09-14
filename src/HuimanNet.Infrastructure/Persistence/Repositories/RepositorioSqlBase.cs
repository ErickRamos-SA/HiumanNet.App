using HuimanNet.Infrastructure.Persistence.Connections;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base común de los repositorios ADO.NET: crea comandos ya enlazados a la
/// conexión y a la transacción de la petición en curso.
/// </summary>
/// <remarks>
/// Compatible con Native AOT: no usa reflexión, emisión de IL ni convenciones
/// de mapeo automáticas. Todo el SQL es explícito y todos los parámetros van
/// tipados (ESPECIFICACION.md §6.1).
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
    /// Crea un comando enlazado a la conexión y a la transacción de la petición.
    /// </summary>
    /// <param name="sql">Sentencia a ejecutar. Siempre con parámetros: nunca concatenada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El comando listo para añadirle parámetros y ejecutarse.</returns>
    protected async Task<SqlCommand> CrearComandoAsync(
        string sql, CancellationToken cancellationToken)
    {
        SqlConnection conexion = await _sesion.ObtenerConexionAsync(cancellationToken);

        SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = _sesion.TiempoDeEsperaComandoSegundos;
        comando.Transaction = _sesion.TransaccionActual;

        return comando;
    }
}
