using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de corridas, resultados y cotejos a sus entidades.
/// </summary>
/// <remarks>
/// Las columnas y su orden los fijan los procedimientos de
/// <c>Procedimientos/Nomina.sql</c> que alimentan estos mapeos: cambiarlas allí
/// obliga a cambiarlos aquí. Los procedimientos de detalle añaden columnas al
/// final de la fila (la clave del período, el nombre de la razón social o de
/// quien calculó la corrida), que lee quien los invoca.
/// </remarks>
public static class LectorDeCorridas
{
    /// <summary>
    /// Construye una corrida a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La corrida rehidratada.</returns>
    public static CorridaDeNomina MapearCorrida(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var totales = new TotalesDeCorrida(
            reader.GetInt32(8), reader.GetDecimal(9), reader.GetDecimal(10), reader.GetDecimal(11), reader.GetDecimal(12),
            reader.GetDecimal(13), reader.GetDecimal(14), reader.GetDecimal(15), reader.GetDecimal(16), reader.GetDecimal(17),
            reader.GetDecimal(18), reader.GetDecimal(19), reader.GetDecimal(20), reader.GetDecimal(21));

        return CorridaDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetInt32(3), (EstadoDeCorrida)reader.GetByte(4),
            reader.GetFieldValue<DateOnly>(5), reader.GetDateTimeOffset(6), reader.GetGuid(7), totales, reader.GetInt64(22),
            reader.IsDBNull(23) ? null : reader.GetString(23), reader.IsDBNull(24) ? null : reader.GetString(24));
    }

    /// <summary>
    /// Construye el resumen de un resultado a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="inicio">Ordinal de la columna <c>Bruto</c>.</param>
    /// <returns>El resumen.</returns>
    public static ResumenDeResultado MapearResumen(SqlDataReader reader, int inicio)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int o = inicio;

        return new ResumenDeResultado(
            reader.GetDecimal(o), reader.GetDecimal(o + 1), reader.GetDecimal(o + 2), reader.GetDecimal(o + 3),
            reader.GetDecimal(o + 4), reader.GetDecimal(o + 5), reader.GetDecimal(o + 6), reader.GetDecimal(o + 7),
            reader.GetDecimal(o + 8), reader.GetDecimal(o + 9), reader.GetDecimal(o + 10), reader.GetDecimal(o + 11),
            reader.GetDecimal(o + 12), reader.GetDecimal(o + 13), reader.GetDecimal(o + 14), reader.GetDecimal(o + 15),
            reader.GetDecimal(o + 16), reader.GetDecimal(o + 17), reader.GetDecimal(o + 18), reader.GetDecimal(o + 19));
    }

    /// <summary>
    /// Construye un resultado a partir de la fila actual y de sus conceptos.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="conceptos">Detalle de conceptos.</param>
    /// <returns>El resultado rehidratado.</returns>
    public static ResultadoDeNomina MapearResultado(SqlDataReader reader, IReadOnlyList<ValorDeConcepto> conceptos)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ResultadoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5),
            (EsquemaDePago)reader.GetByte(6), reader.GetString(7), reader.GetString(8), (TipoDeMovimiento)reader.GetByte(9),
            MapearResumen(reader, 10), conceptos, reader.IsDBNull(30) ? null : reader.GetString(30));
    }

    /// <summary>
    /// Construye un cotejo a partir de la fila actual y de sus diferencias.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="diferencias">Detalle de diferencias.</param>
    /// <returns>El cotejo rehidratado.</returns>
    public static CotejoDeNomina MapearCotejo(SqlDataReader reader, IReadOnlyList<DiferenciaDeCotejo> diferencias)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return CotejoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetDateTimeOffset(3), reader.GetGuid(4),
            reader.GetString(5), reader.GetDecimal(6), reader.GetInt32(7), reader.GetInt32(8), diferencias);
    }
}
