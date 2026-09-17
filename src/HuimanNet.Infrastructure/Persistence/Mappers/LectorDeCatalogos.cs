using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de los catálogos de cálculo a sus entidades.
/// </summary>
/// <remarks>
/// Las columnas y su orden los fijan los procedimientos de
/// <c>Procedimientos/Catalogos.sql</c> que alimentan estos mapeos: cambiarlas
/// allí obliga a cambiarlos aquí.
/// </remarks>
public static class LectorDeCatalogos
{
    /// <summary>Separador de los alias de cotejo en la columna <c>AliasDeCotejo</c>.</summary>
    public const char SeparadorDeAlias = '|';

    /// <summary>
    /// Construye un parámetro a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El parámetro rehidratado.</returns>
    public static ParametroDeCalculo MapearParametro(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ParametroDeCalculo.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4),
            reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.GetFieldValue<DateOnly>(7),
            reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8), reader.GetDateTimeOffset(9));
    }

    /// <summary>
    /// Construye un rango a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El identificador de la tabla y el rango.</returns>
    public static (Guid TablaId, RangoDeTabla Rango) MapearRango(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return (reader.GetGuid(0), new RangoDeTabla(
            reader.GetDecimal(2), reader.IsDBNull(3) ? null : reader.GetDecimal(3), reader.GetDecimal(4),
            reader.GetDecimal(5), reader.GetDecimal(6)));
    }

    /// <summary>
    /// Construye una tabla a partir de la fila actual y de sus rangos.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="rangos">Rangos de la tabla.</param>
    /// <returns>La tabla rehidratada.</returns>
    public static TablaDeRangos MapearTabla(SqlDataReader reader, IEnumerable<RangoDeTabla> rangos)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return TablaDeRangos.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetFieldValue<DateOnly>(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
            reader.GetDateTimeOffset(6), rangos);
    }

    /// <summary>
    /// Construye un concepto a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El concepto rehidratado.</returns>
    public static ConceptoDeNomina MapearConcepto(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ConceptoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            (TipoDeConcepto)reader.GetByte(4), (EsquemasDePago)reader.GetByte(5), reader.GetInt32(6), reader.GetString(7),
            reader.GetBoolean(8), reader.GetBoolean(9), reader.IsDBNull(10) ? null : reader.GetGuid(10), reader.GetDateTimeOffset(11),
            reader.IsDBNull(12) ? [] : reader.GetString(12).Split(SeparadorDeAlias, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Construye una sección de explicación a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La sección rehidratada.</returns>
    public static ExplicacionDeCalculo MapearExplicacion(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ExplicacionDeCalculo.Rehidratar(
            reader.GetGuid(0), (EsquemaDePago)reader.GetByte(1), (Idioma)reader.GetByte(2), reader.GetInt32(3),
            reader.GetString(4), reader.GetString(5), reader.GetDateTimeOffset(6));
    }
}
