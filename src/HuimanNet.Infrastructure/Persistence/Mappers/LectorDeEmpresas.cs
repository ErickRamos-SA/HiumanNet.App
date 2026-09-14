using HuimanNet.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Empresas</c> a entidades <see cref="Empresa"/>.
/// </summary>
/// <remarks>Mapeo manual por ordinal, sin reflexión.</remarks>
public static class LectorDeEmpresas
{
    /// <summary>
    /// Obtiene la lista de columnas que debe proyectar cualquier consulta que
    /// alimente a <see cref="Mapear"/>.
    /// </summary>
    /// <value>Fragmento SQL con los nombres de columna en el orden esperado.</value>
    public const string Columnas =
        "e.Id, e.RazonSocial, e.IdentificadorFiscal, e.PrefijoContenedor, e.Activa, e.FechaAlta";

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
