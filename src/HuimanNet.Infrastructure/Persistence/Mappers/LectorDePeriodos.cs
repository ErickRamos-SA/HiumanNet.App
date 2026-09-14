using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Periodos</c> a entidades <see cref="PeriodoCarga"/>.
/// </summary>
/// <remarks>Mapeo manual por ordinal, sin reflexión.</remarks>
public static class LectorDePeriodos
{
    /// <summary>
    /// Obtiene la lista de columnas que debe proyectar cualquier consulta que
    /// alimente a <see cref="Mapear"/>.
    /// </summary>
    /// <value>Fragmento SQL con los nombres de columna en el orden esperado.</value>
    public const string Columnas =
        "p.Id, p.EmpresaId, p.Anio, p.Mes, p.Consecutivo, p.Descripcion, p.Estado, " +
        "p.FechaApertura, p.FechaLimiteCarga, p.FechaCierre";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El período rehidratado.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="reader"/> es <c>null</c>.
    /// </exception>
    public static PeriodoCarga Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return PeriodoCarga.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            PeriodoCalendario.Crear(reader.GetInt16(2), reader.GetByte(3), reader.GetByte(4)),
            reader.GetString(5),
            (EstadoPeriodo)reader.GetByte(6),
            reader.GetDateTimeOffset(7),
            reader.IsDBNull(8) ? null : reader.GetDateTimeOffset(8),
            reader.IsDBNull(9) ? null : reader.GetDateTimeOffset(9));
    }
}
