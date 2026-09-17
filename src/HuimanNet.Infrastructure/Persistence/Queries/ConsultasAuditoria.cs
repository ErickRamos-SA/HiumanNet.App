using System.Data;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Common;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de la bitácora de auditoría, con filtros y paginación
/// resueltos en SQL Server.
/// </summary>
public sealed class ConsultasAuditoria : RepositorioSqlBase, IConsultasAuditoria
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasAuditoria"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasAuditoria(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<PaginaDto<RegistroAuditoriaDto>> ConsultarAsync(
        FiltroDeAuditoria filtro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        // Dos conjuntos de resultados en un único viaje de red: el total y la página.
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Auditoria.Consultar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Desde", SqlDbType.DateTimeOffset) { Value = filtro.Desde });
        comando.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.DateTimeOffset) { Value = filtro.Hasta });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)filtro.EmpresaId ?? DBNull.Value,
        });
        comando.Parameters.Add(new SqlParameter("@Accion", SqlDbType.TinyInt)
        {
            Value = filtro.Accion is null ? DBNull.Value : (byte)filtro.Accion.Value,
        });
        comando.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)filtro.UsuarioId ?? DBNull.Value,
        });
        comando.Parameters.Add(new SqlParameter("@Omitir", SqlDbType.Int)
        {
            Value = (filtro.Pagina - 1) * filtro.TamanoPagina,
        });
        comando.Parameters.Add(new SqlParameter("@Tomar", SqlDbType.Int) { Value = filtro.TamanoPagina });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        int total = await reader.ReadAsync(cancellationToken) ? (int)reader.GetInt64(0) : 0;

        var registros = new List<RegistroAuditoriaDto>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                registros.Add(new RegistroAuditoriaDto(
                    reader.GetGuid(0),
                    reader.GetDateTimeOffset(1),
                    (AccionAuditada)reader.GetByte(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetGuid(6),
                    reader.GetBoolean(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8)));
            }
        }

        int totalPaginas = filtro.TamanoPagina <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanoPagina));

        return new PaginaDto<RegistroAuditoriaDto>(
            registros, total, filtro.Pagina, filtro.TamanoPagina, totalPaginas);
    }
}
