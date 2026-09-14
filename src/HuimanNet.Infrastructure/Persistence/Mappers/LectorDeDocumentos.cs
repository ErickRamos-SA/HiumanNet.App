using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Documentos</c> a entidades <see cref="Documento"/>.
/// </summary>
/// <remarks>
/// Mapeo escrito a mano y por <b>ordinal</b>: sin reflexión y sin buscar
/// columnas por nombre en cada fila. El orden de <see cref="Columnas"/> y el de
/// las lecturas de <see cref="Mapear"/> deben mantenerse sincronizados; por eso
/// viven juntos en el mismo archivo.
/// </remarks>
public static class LectorDeDocumentos
{
    /// <summary>
    /// Obtiene la lista de columnas que debe proyectar cualquier consulta que
    /// alimente a <see cref="Mapear"/>.
    /// </summary>
    /// <value>Fragmento SQL con los nombres de columna en el orden esperado.</value>
    public const string Columnas =
        "d.Id, d.EmpresaId, d.PeriodoId, d.Tipo, d.NombreOriginal, d.RutaBlob, " +
        "d.TamanoBytes, d.Estado, d.HuellaSha256, d.CargadoPorUsuarioId, " +
        "d.FechaSolicitud, d.FechaCargaConfirmada, d.FechaEscaneo, d.MotivoCuarentena";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El documento rehidratado.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="reader"/> es <c>null</c>.
    /// </exception>
    public static Documento Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return Documento.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            (TipoDocumento)reader.GetByte(3),
            NombreArchivo.Crear(reader.GetString(4)),
            reader.GetString(5),
            TamanoArchivo.DesdeBytes(reader.GetInt64(6)),
            (EstadoDocumento)reader.GetByte(7),
            reader.IsDBNull(8) ? default : HuellaArchivo.Crear(reader.GetString(8)),
            reader.GetGuid(9),
            reader.GetDateTimeOffset(10),
            reader.IsDBNull(11) ? null : reader.GetDateTimeOffset(11),
            reader.IsDBNull(12) ? null : reader.GetDateTimeOffset(12),
            reader.IsDBNull(13) ? null : reader.GetString(13));
    }
}
