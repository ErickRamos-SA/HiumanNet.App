using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Conexión y transacción compartidas por todos los repositorios de una misma
/// petición.
/// </summary>
public interface ISesionSql
{
    /// <summary>
    /// Obtiene la conexión de la petición, abriéndola la primera vez.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La conexión abierta y compartida.</returns>
    ValueTask<SqlConnection> ObtenerConexionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la transacción abierta, si el caso de uso inició una.
    /// </summary>
    /// <value><c>null</c> cuando las escrituras se ejecutan en confirmación automática.</value>
    SqlTransaction? TransaccionActual { get; }

    /// <summary>
    /// Obtiene el tiempo máximo de ejecución que debe aplicarse a cada comando.
    /// </summary>
    /// <value>Valor en segundos, tomado de la configuración.</value>
    int TiempoDeEsperaComandoSegundos { get; }
}
