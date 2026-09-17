using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.RazonesSociales</c> a entidades <see cref="RazonSocial"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Las columnas y su orden los fijan
/// los procedimientos de <c>Procedimientos/RazonesSociales.sql</c> que
/// alimentan este lector: cambiarlas allí obliga a cambiar este mapeo.
/// </remarks>
public static class LectorDeRazonesSociales
{
    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La razón social rehidratada.</returns>
    public static RazonSocial Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var configuracion = new ConfiguracionDeRazonSocial(
            (TipoDeServicio)reader.GetByte(6),
            reader.GetBoolean(7),
            reader.GetBoolean(8),
            (ModalidadDeComision)reader.GetByte(9),
            reader.GetDecimal(10),
            (ZonaIsn)reader.GetByte(11),
            reader.IsDBNull(12) ? null : reader.GetDecimal(12),
            reader.GetDecimal(13),
            reader.IsDBNull(14) ? null : reader.GetDecimal(14));

        return RazonSocial.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            (ZonaSalarioMinimo)reader.GetByte(5),
            configuracion,
            reader.IsDBNull(15) ? null : reader.GetString(15),
            reader.GetBoolean(16),
            reader.GetDateTimeOffset(17));
    }
}
