using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de razones sociales basado en ADO.NET sobre SQL Server.
/// </summary>
/// <remarks>Toda lectura filtra por empresa en la cláusula <c>WHERE</c>.</remarks>
public sealed class RazonSocialRepository : RepositorioSqlBase, IRazonSocialRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RazonSocialRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public RazonSocialRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<RazonSocial?> ObtenerPorIdAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeRazonesSociales.Columnas}
            FROM   dbo.RazonesSociales AS r
            WHERE  r.Id = @Id AND r.EmpresaId = @EmpresaId;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeRazonesSociales.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RazonSocial>> ListarPorEmpresaAsync(
        Guid empresaId, bool soloActivas, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeRazonesSociales.Columnas}
            FROM   dbo.RazonesSociales AS r
            WHERE  r.EmpresaId = @EmpresaId AND (@SoloActivas = 0 OR r.Activa = 1)
            ORDER BY r.Nombre;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@SoloActivas", SqlDbType.Bit) { Value = soloActivas });

        var lista = new List<RazonSocial>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeRazonesSociales.Mapear(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(RazonSocial razonSocial, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(razonSocial);

        const string sql = """
            INSERT INTO dbo.RazonesSociales
                (Id, EmpresaId, Nombre, Rfc, RegistroPatronal, Zona, TipoServicio, SubsidioAbsorbido,
                 AplicaFaltasProporcionales, ModalidadComision, PorcentajeComision, ZonaIsn, TasaIva,
                 PorcentajeOtrosCostos, PrimaRiesgo, BancoDispersor, Activa, FechaAlta)
            VALUES
                (@Id, @EmpresaId, @Nombre, @Rfc, @RegistroPatronal, @Zona, @TipoServicio, @SubsidioAbsorbido,
                 @AplicaFaltas, @ModalidadComision, @PorcentajeComision, @ZonaIsn, @TasaIva,
                 @PorcentajeOtros, @PrimaRiesgo, @BancoDispersor, @Activa, @FechaAlta);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, razonSocial);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = razonSocial.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = razonSocial.FechaAlta });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(RazonSocial razonSocial, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(razonSocial);

        const string sql = """
            UPDATE dbo.RazonesSociales
            SET    Nombre = @Nombre, Rfc = @Rfc, RegistroPatronal = @RegistroPatronal, Zona = @Zona,
                   TipoServicio = @TipoServicio, SubsidioAbsorbido = @SubsidioAbsorbido,
                   AplicaFaltasProporcionales = @AplicaFaltas, ModalidadComision = @ModalidadComision,
                   PorcentajeComision = @PorcentajeComision, ZonaIsn = @ZonaIsn, TasaIva = @TasaIva,
                   PorcentajeOtrosCostos = @PorcentajeOtros, PrimaRiesgo = @PrimaRiesgo,
                   BancoDispersor = @BancoDispersor, Activa = @Activa
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, razonSocial);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AgregarParametros(SqlCommand comando, RazonSocial r)
    {
        ConfiguracionDeRazonSocial c = r.Configuracion;

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = r.Id });
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 200) { Value = r.Nombre });
        comando.Parameters.Add(new SqlParameter("@Rfc", SqlDbType.NVarChar, 20) { Value = r.Rfc });
        comando.Parameters.Add(new SqlParameter("@RegistroPatronal", SqlDbType.NVarChar, 30) { Value = (object?)r.RegistroPatronal ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Zona", SqlDbType.TinyInt) { Value = (byte)r.Zona });
        comando.Parameters.Add(new SqlParameter("@TipoServicio", SqlDbType.TinyInt) { Value = (byte)c.TipoDeServicio });
        comando.Parameters.Add(new SqlParameter("@SubsidioAbsorbido", SqlDbType.Bit) { Value = c.SubsidioAbsorbido });
        comando.Parameters.Add(new SqlParameter("@AplicaFaltas", SqlDbType.Bit) { Value = c.AplicaFaltasProporcionales });
        comando.Parameters.Add(new SqlParameter("@ModalidadComision", SqlDbType.TinyInt) { Value = (byte)c.ModalidadDeComision });
        comando.Parameters.Add(Decimal("@PorcentajeComision", c.PorcentajeComision));
        comando.Parameters.Add(new SqlParameter("@ZonaIsn", SqlDbType.TinyInt) { Value = (byte)c.ZonaIsn });
        comando.Parameters.Add(Decimal("@TasaIva", c.TasaIva));
        comando.Parameters.Add(Decimal("@PorcentajeOtros", c.PorcentajeOtrosCostos));
        comando.Parameters.Add(Decimal("@PrimaRiesgo", c.PrimaDeRiesgo));
        comando.Parameters.Add(new SqlParameter("@BancoDispersor", SqlDbType.NVarChar, 100) { Value = (object?)r.BancoDispersor ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Activa", SqlDbType.Bit) { Value = r.Activa });
    }

    private static SqlParameter Decimal(string nombre, decimal? valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 19, Scale = 8, Value = (object?)valor ?? DBNull.Value };
}
