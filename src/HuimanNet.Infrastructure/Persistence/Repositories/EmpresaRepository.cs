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
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpresas.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Empresa>> ListarAsync(
        bool soloActivas = true, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.Listar, cancellationToken);
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

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.Insertar, cancellationToken);

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

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empresas.Actualizar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empresa.Id });
        comando.Parameters.Add(new SqlParameter("@RazonSocial", SqlDbType.NVarChar, 200) { Value = empresa.RazonSocial });
        comando.Parameters.Add(new SqlParameter("@IdentificadorFiscal", SqlDbType.NVarChar, 50) { Value = empresa.IdentificadorFiscal });
        comando.Parameters.Add(new SqlParameter("@Activa", SqlDbType.Bit) { Value = empresa.Activa });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
