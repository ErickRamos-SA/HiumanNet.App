using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Incidencias</c> a entidades <see cref="Incidencia"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Las columnas y su orden los fijan
/// los procedimientos de <c>Procedimientos/Incidencias.sql</c>: cambiarlas allí
/// obliga a cambiar este mapeo.
/// </remarks>
public static class LectorDeIncidencias
{
    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La incidencia rehidratada.</returns>
    public static Incidencia Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return Incidencia.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), MapearDatos(reader, 4),
            reader.GetGuid(28), reader.GetDateTimeOffset(29));
    }

    /// <summary>
    /// Lee las cantidades e importes de una incidencia a partir de un ordinal inicial.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="inicio">Ordinal de <c>DiasPeriodo</c>.</param>
    /// <returns>Los datos de la incidencia.</returns>
    public static DatosDeIncidencia MapearDatos(SqlDataReader reader, int inicio)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int o = inicio;

        return new DatosDeIncidencia(
            reader.GetDecimal(o), reader.GetDecimal(o + 1), reader.GetDecimal(o + 2), reader.GetDecimal(o + 3),
            reader.GetDecimal(o + 4), reader.GetDecimal(o + 5), reader.GetDecimal(o + 6), reader.GetDecimal(o + 7),
            reader.GetDecimal(o + 8), reader.GetDecimal(o + 9), reader.GetDecimal(o + 10), reader.GetDecimal(o + 11),
            reader.GetDecimal(o + 12), reader.GetDecimal(o + 13), reader.GetDecimal(o + 14), reader.GetDecimal(o + 15),
            reader.GetDecimal(o + 16), reader.GetDecimal(o + 17), reader.GetDecimal(o + 18), reader.GetDecimal(o + 19),
            reader.GetDecimal(o + 20),
            reader.IsDBNull(o + 21) ? null : reader.GetDecimal(o + 21),
            (TipoDeMovimiento)reader.GetByte(o + 22),
            reader.IsDBNull(o + 23) ? null : reader.GetString(o + 23));
    }
}
