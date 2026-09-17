using System.Data;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de documentos: proyecta directamente a
/// <see cref="DocumentoDto"/> en una sola consulta.
/// </summary>
/// <remarks>
/// El procedimiento resuelve la unión con <c>dbo.Usuarios</c> que la interfaz
/// necesita para mostrar quién cargó cada documento, sin obligar al dominio a
/// conocer esa relación.
/// </remarks>
public sealed class ConsultasDocumentos : RepositorioSqlBase, IConsultasDocumentos
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasDocumentos"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasDocumentos(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentoDto>> ListarPorPeriodoAsync(
        Guid periodoId,
        Guid empresaId,
        TipoDocumento? tipo,
        bool soloDescargables,
        CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Documentos.ListarDetallePorPeriodo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.TinyInt)
        {
            Value = tipo is null ? DBNull.Value : (byte)tipo.Value,
        });
        comando.Parameters.Add(new SqlParameter("@SoloDescargables", SqlDbType.Bit) { Value = soloDescargables });
        comando.Parameters.Add(new SqlParameter("@EstadoDisponible", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoDocumento.Disponible,
        });
        comando.Parameters.Add(new SqlParameter("@EstadoDescartado", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoDocumento.Descartado,
        });

        var documentos = new List<DocumentoDto>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var estado = (EstadoDocumento)reader.GetByte(6);

            documentos.Add(new DocumentoDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                (TipoDocumento)reader.GetByte(3),
                reader.GetString(4),
                reader.GetInt64(5),
                estado,
                reader.GetDateTimeOffset(7),
                reader.IsDBNull(8) ? null : reader.GetDateTimeOffset(8),
                reader.GetString(9),
                estado == EstadoDocumento.Disponible));
        }

        return documentos;
    }
}
