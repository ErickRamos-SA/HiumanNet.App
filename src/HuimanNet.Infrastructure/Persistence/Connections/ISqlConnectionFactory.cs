using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Fábrica de conexiones a SQL Server.
/// </summary>
/// <remarks>
/// Aísla la cadena de conexión del resto de la infraestructura y permite
/// sustituirla en pruebas. Las conexiones se agrupan en el <i>pool</i> de
/// ADO.NET: crear una instancia es barato.
/// </remarks>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Crea una conexión sin abrir.
    /// </summary>
    /// <returns>Una <see cref="SqlConnection"/> cerrada, lista para abrirse.</returns>
    /// <remarks>La conexión es propiedad de quien la crea, que debe liberarla.</remarks>
    SqlConnection Crear();

    /// <summary>
    /// Obtiene el tiempo máximo de ejecución que debe aplicarse a cada comando.
    /// </summary>
    /// <value>Valor en segundos, tomado de la configuración.</value>
    int TiempoDeEsperaComandoSegundos { get; }
}
