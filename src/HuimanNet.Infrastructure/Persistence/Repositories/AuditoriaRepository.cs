using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Escritura de la bitácora de auditoría sobre SQL Server.
/// </summary>
/// <remarks>
/// Sólo inserta: los asientos son inmutables y no se borran. La retención la
/// gobierna la política de auditoría, no la aplicación (ARQUITECTURA.md §6.3).
/// </remarks>
public sealed class AuditoriaRepository : RepositorioSqlBase, IAuditoriaRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AuditoriaRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public AuditoriaRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(
        RegistroAuditoria registro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Auditoria.Insertar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = registro.Id });
        comando.Parameters.Add(new SqlParameter("@Momento", SqlDbType.DateTimeOffset) { Value = registro.Momento });
        comando.Parameters.Add(new SqlParameter("@Accion", SqlDbType.TinyInt) { Value = (byte)registro.Accion });
        comando.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.UniqueIdentifier) { Value = registro.UsuarioId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)registro.EmpresaId ?? DBNull.Value,
        });
        comando.Parameters.Add(new SqlParameter("@RecursoTipo", SqlDbType.NVarChar, 64) { Value = registro.RecursoTipo });
        comando.Parameters.Add(new SqlParameter("@RecursoId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)registro.RecursoId ?? DBNull.Value,
        });
        comando.Parameters.Add(new SqlParameter("@Exito", SqlDbType.Bit) { Value = registro.Exito });
        comando.Parameters.Add(new SqlParameter("@Detalle", SqlDbType.NVarChar, 1000)
        {
            Value = (object?)registro.Detalle ?? DBNull.Value,
        });
        comando.Parameters.Add(new SqlParameter("@DireccionIp", SqlDbType.NVarChar, 64)
        {
            Value = (object?)registro.DireccionIp ?? DBNull.Value,
        });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
