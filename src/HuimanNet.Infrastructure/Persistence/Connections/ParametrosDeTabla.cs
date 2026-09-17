using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Construye los parámetros de tabla con los que los procedimientos reciben una
/// colección completa de filas.
/// </summary>
/// <remarks>
/// Las filas viajan como <see cref="SqlDataRecord"/>, que es la forma nativa de
/// SqlClient para estos parámetros: no usa reflexión ni <c>DataTable</c>, así
/// que es compatible con Native AOT. Un <see cref="System.Data.Common.DbDataReader"/>
/// no sirve aquí —el controlador le pide <c>GetSchemaTable</c> para deducir las
/// columnas—; ese camino se reserva para <c>SqlBulkCopy</c>.
/// </remarks>
public static class ParametrosDeTabla
{
    /// <summary>
    /// Crea un parámetro de tabla a partir de una colección de filas.
    /// </summary>
    /// <typeparam name="T">Tipo de las filas.</typeparam>
    /// <param name="nombre">Nombre del parámetro, con arroba.</param>
    /// <param name="tipo">Nombre del tipo de tabla en la base de datos.</param>
    /// <param name="columnas">Columnas del tipo, en su orden de declaración.</param>
    /// <param name="filas">Filas a enviar; si está vacía, el procedimiento recibe una tabla vacía.</param>
    /// <param name="escribir">Vuelca una fila en el registro que viaja al servidor.</param>
    /// <returns>El parámetro listo para añadirse al comando.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="columnas"/>, <paramref name="filas"/> o
    /// <paramref name="escribir"/> son <c>null</c>.
    /// </exception>
    public static SqlParameter Crear<T>(
        string nombre,
        string tipo,
        SqlMetaData[] columnas,
        IReadOnlyCollection<T> filas,
        Action<SqlDataRecord, T> escribir)
    {
        ArgumentNullException.ThrowIfNull(columnas);
        ArgumentNullException.ThrowIfNull(filas);
        ArgumentNullException.ThrowIfNull(escribir);

        return new SqlParameter(nombre, SqlDbType.Structured)
        {
            TypeName = tipo,

            // SqlClient rechaza tanto una enumeración vacía como DBNull en un
            // parámetro de tabla: para una tabla sin filas hay que enviar una
            // referencia nula, que el servidor recibe como tabla vacía.
            Value = filas.Count == 0 ? null : Registros(columnas, filas, escribir),
        };
    }

    /// <summary>
    /// Recorre las filas reutilizando un único registro, como espera SqlClient.
    /// </summary>
    /// <typeparam name="T">Tipo de las filas.</typeparam>
    /// <param name="columnas">Columnas del tipo de tabla.</param>
    /// <param name="filas">Filas a enviar.</param>
    /// <param name="escribir">Vuelca una fila en el registro.</param>
    /// <returns>Los registros, en el orden de la colección.</returns>
    private static IEnumerable<SqlDataRecord> Registros<T>(
        SqlMetaData[] columnas, IReadOnlyCollection<T> filas, Action<SqlDataRecord, T> escribir)
    {
        // El controlador consume cada registro antes de pedir el siguiente, así
        // que una sola instancia basta y evita una asignación por fila.
        var registro = new SqlDataRecord(columnas);

        foreach (T fila in filas)
        {
            escribir(registro, fila);
            yield return registro;
        }
    }
}
