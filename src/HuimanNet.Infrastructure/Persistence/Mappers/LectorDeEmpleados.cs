using HuimanNet.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Empleados</c> a entidades <see cref="Empleado"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Las columnas y su orden los fijan
/// los procedimientos de <c>Procedimientos/Empleados.sql</c> que alimentan este
/// lector: cambiarlas allí obliga a cambiar este mapeo.
/// </remarks>
public static class LectorDeEmpleados
{
    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El empleado rehidratado.</returns>
    public static Empleado Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var datos = new DatosPersonales(
            reader.GetString(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetFieldValue<DateOnly>(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11));

        return Empleado.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), datos, reader.GetBoolean(12), reader.GetDateTimeOffset(13));
    }
}
