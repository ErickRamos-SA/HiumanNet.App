using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de empresas cliente basado en ADO.NET sobre SQL Server.
/// </summary>
public sealed class EmpresaRepository : RepositorioSqlBase, IEmpresaRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmpresaRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public EmpresaRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeEmpresas.Columnas}
            FROM   dbo.Empresas AS e
            WHERE  e.Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpresas.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Empresa>> ListarAsync(
        bool soloActivas = true, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeEmpresas.Columnas}
            FROM   dbo.Empresas AS e
            WHERE  (@SoloActivas = 0 OR e.Activa = 1)
            ORDER BY e.RazonSocial ASC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@SoloActivas", SqlDbType.Bit) { Value = soloActivas });

        var empresas = new List<Empresa>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            empresas.Add(LectorDeEmpresas.Mapear(reader));
        }

        return empresas;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        const string sql = """
            INSERT INTO dbo.Empresas
                (Id, RazonSocial, IdentificadorFiscal, PrefijoContenedor, Activa, FechaAlta)
            VALUES
                (@Id, @RazonSocial, @IdentificadorFiscal, @PrefijoContenedor, @Activa, @FechaAlta);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresa.Id });
        comando.Parameters.Add(new SqlParameter("@RazonSocial", SqlDbType.NVarChar, 200) { Value = empresa.RazonSocial });
        comando.Parameters.Add(new SqlParameter("@IdentificadorFiscal", SqlDbType.NVarChar, 50) { Value = empresa.IdentificadorFiscal });
        comando.Parameters.Add(new SqlParameter("@PrefijoContenedor", SqlDbType.NVarChar, 100) { Value = empresa.PrefijoContenedor });
        comando.Parameters.Add(new SqlParameter("@Activa", SqlDbType.Bit) { Value = empresa.Activa });
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = empresa.FechaAlta });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        const string sql = """
            UPDATE dbo.Empresas
            SET    RazonSocial = @RazonSocial,
                   IdentificadorFiscal = @IdentificadorFiscal,
                   Activa = @Activa
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresa.Id });
        comando.Parameters.Add(new SqlParameter("@RazonSocial", SqlDbType.NVarChar, 200) { Value = empresa.RazonSocial });
        comando.Parameters.Add(new SqlParameter("@IdentificadorFiscal", SqlDbType.NVarChar, 50) { Value = empresa.IdentificadorFiscal });
        comando.Parameters.Add(new SqlParameter("@Activa", SqlDbType.Bit) { Value = empresa.Activa });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
