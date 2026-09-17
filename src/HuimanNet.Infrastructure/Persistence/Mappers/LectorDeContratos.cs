using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Contratos</c> a entidades <see cref="Contrato"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Las columnas y su orden los fijan
/// los procedimientos de contratos de <c>Procedimientos/Empleados.sql</c>:
/// cambiarlas allí obliga a cambiar este mapeo. En el detalle del empleado, el
/// nombre de la razón social viaja en el ordinal 26, después de estas columnas.
/// </remarks>
public static class LectorDeContratos
{
    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El contrato rehidratado.</returns>
    public static Contrato Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var condiciones = new CondicionesDeContrato(
            reader.GetDecimal(9),
            reader.GetDecimal(10),
            reader.GetDecimal(11),
            (ZonaSalarioMinimo)reader.GetByte(12),
            new CreditoInfonavit((TipoDeCreditoInfonavit)reader.GetByte(13), reader.GetDecimal(14), reader.GetDecimal(15)),
            reader.GetDecimal(16),
            reader.GetDecimal(17),
            reader.GetDecimal(18),
            reader.GetDecimal(19),
            reader.GetDecimal(20),
            reader.GetBoolean(21),
            reader.GetBoolean(25));

        return Contrato.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            (EsquemaDePago)reader.GetByte(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            condiciones,
            reader.GetFieldValue<DateOnly>(22),
            reader.IsDBNull(23) ? null : reader.GetFieldValue<DateOnly>(23),
            reader.GetDateTimeOffset(24));
    }
}
