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
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.ListarResumen, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@SoloActivas", SqlDbType.Bit) { Value = soloActivas });

        var empresas = new List<EmpresaDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            empresas.Add(MapearResumen(reader));
        }

        return empresas;
    }

    /// <inheritdoc/>
    public async Task<EmpresaDto?> ObtenerAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.ObtenerResumen, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapearResumen(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmpresaDetalleDto>> ListarDetalleAsync(bool soloActivas, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.ListarDetalle, cancellationToken);
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
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.ObtenerDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapearDetalle(reader) : null;
    }

    /// <summary>Proyecta una fila de <c>Empresas_ListarResumen</c> a su DTO.</summary>
    /// <param name="reader">Lector situado en la fila.</param>
    /// <returns>El resumen de la empresa.</returns>
    private static EmpresaDto MapearResumen(SqlDataReader reader)
        => new(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2));

    /// <summary>Proyecta una fila de <c>Empresas_ListarDetalle</c> a su DTO.</summary>
    /// <param name="reader">Lector situado en la fila.</param>
    /// <returns>El detalle de la empresa.</returns>
    private static EmpresaDetalleDto MapearDetalle(SqlDataReader reader)
        => new(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetDateTimeOffset(4),
            (int)reader.GetInt64(5), (int)reader.GetInt64(6), (int)reader.GetInt64(7));
}
