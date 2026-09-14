using System.Data;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura del catálogo de empresas.
/// </summary>
/// <remarks>
/// La proyección básica omite el identificador fiscal; sólo el detalle
/// administrativo, reservado a roles transversales, lo incluye.
/// </remarks>
public sealed class ConsultasEmpresas : RepositorioSqlBase, IConsultasEmpresas
{
    private const string ProyeccionDeDetalle = """
        SELECT e.Id, e.RazonSocial, e.IdentificadorFiscal, e.Activa, e.FechaAlta,
               (SELECT COUNT_BIG(1) FROM dbo.RazonesSociales AS r WHERE r.EmpresaId = e.Id AND r.Activa = 1),
               (SELECT COUNT_BIG(1) FROM dbo.Empleados AS m WHERE m.EmpresaId = e.Id AND m.Activo = 1),
               (SELECT COUNT_BIG(1) FROM dbo.Usuarios AS u WHERE u.EmpresaId = e.Id AND u.Activo = 1)
        FROM   dbo.Empresas AS e
        """;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasEmpresas"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasEmpresas(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmpresaDto>> ListarAsync(bool soloActivas, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT e.Id, e.RazonSocial, e.Activa
            FROM   dbo.Empresas AS e
            WHERE  (@SoloActivas = 0 OR e.Activa = 1)
            ORDER BY e.RazonSocial;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@SoloActivas", SqlDbType.Bit) { Value = soloActivas });

        var empresas = new List<EmpresaDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            empresas.Add(new EmpresaDto(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return empresas;
    }

    /// <inheritdoc/>
    public async Task<EmpresaDto?> ObtenerAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearComandoAsync(
            "SELECT e.Id, e.RazonSocial, e.Activa FROM dbo.Empresas AS e WHERE e.Id = @Id;", cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? new EmpresaDto(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2))
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmpresaDetalleDto>> ListarDetalleAsync(bool soloActivas, CancellationToken cancellationToken = default)
    {
        string sql = $"{ProyeccionDeDetalle} WHERE (@SoloActivas = 0 OR e.Activa = 1) ORDER BY e.RazonSocial;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@SoloActivas", SqlDbType.Bit) { Value = soloActivas });

        var lista = new List<EmpresaDetalleDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(MapearDetalle(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<EmpresaDetalleDto?> ObtenerDetalleAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"{ProyeccionDeDetalle} WHERE e.Id = @Id;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapearDetalle(reader) : null;
    }

    private static EmpresaDetalleDto MapearDetalle(SqlDataReader reader)
        => new(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetDateTimeOffset(4),
            (int)reader.GetInt64(5), (int)reader.GetInt64(6), (int)reader.GetInt64(7));
}
