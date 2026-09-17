using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Serializacion;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de cotejos de nómina.
/// </summary>
public sealed class CotejoRepository : RepositorioSqlBase, ICotejoRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CotejoRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public CotejoRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(CotejoDeNomina cotejo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cotejo);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Nomina.CotejoInsertar, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = cotejo.Id });
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = cotejo.CorridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = cotejo.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = cotejo.FechaCotejo });
        comando.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.UniqueIdentifier) { Value = cotejo.UsuarioId });
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 255) { Value = cotejo.NombreArchivo });
        comando.Parameters.Add(new SqlParameter("@Tolerancia", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = cotejo.ToleranciaAbsoluta });
        comando.Parameters.Add(new SqlParameter("@Total", SqlDbType.Int) { Value = cotejo.TotalComparaciones });
        comando.Parameters.Add(new SqlParameter("@Fuera", SqlDbType.Int) { Value = cotejo.TotalFueraDeTolerancia });
        comando.Parameters.Add(new SqlParameter("@Diferencias", SqlDbType.NVarChar, -1) { Value = SerializadorDeNomina.SerializarDiferencias(cotejo.Diferencias) });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CotejoDeNomina?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Nomina.CotejoObtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? LectorDeCorridas.MapearCotejo(reader, SerializadorDeNomina.LeerDiferencias(reader.GetString(9)))
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CotejoDeNomina>> ListarPorCorridaAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CotejosListarPorCorrida, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        var lista = new List<CotejoDeNomina>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeCorridas.MapearCotejo(reader, []));
        }

        return lista;
    }
}
