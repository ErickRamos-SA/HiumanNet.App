using HuimanNet.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Empresas</c> a entidades <see cref="Empresa"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Las columnas y su orden los fija el
/// procedimiento que alimenta el lector (<c>Empresas_Obtener</c> y
/// <c>Empresas_Listar</c>, en <c>Procedimientos/Empresas.sql</c>): cambiarlas
/// allí obliga a cambiar este mapeo.
/// </remarks>
public static class LectorDeEmpresas
{
    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La empresa rehidratada.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="reader"/> es <c>null</c>.
    /// </exception>
    public static Empresa Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return Empresa.Rehidratar(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetBoolean(4),
            reader.GetDateTimeOffset(5));
    }
}
