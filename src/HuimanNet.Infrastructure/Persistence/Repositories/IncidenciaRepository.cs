using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de incidencias de período basado en ADO.NET sobre SQL Server.
/// </summary>
public sealed class IncidenciaRepository : RepositorioSqlBase, IIncidenciaRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="IncidenciaRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public IncidenciaRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<Incidencia?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {LectorDeIncidencias.Columnas} FROM dbo.Incidencias AS i WHERE i.Id = @Id AND i.EmpresaId = @EmpresaId;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeIncidencias.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<Incidencia?> ObtenerPorContratoAsync(
        Guid periodoId, Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeIncidencias.Columnas}
            FROM   dbo.Incidencias AS i
            WHERE  i.PeriodoId = @PeriodoId AND i.ContratoId = @ContratoId AND i.EmpresaId = @EmpresaId;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@ContratoId", SqlDbType.UniqueIdentifier) { Value = contratoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeIncidencias.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Incidencia>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeIncidencias.Columnas}
            FROM   dbo.Incidencias AS i
            WHERE  i.PeriodoId = @PeriodoId AND i.EmpresaId = @EmpresaId;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        var lista = new List<Incidencia>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeIncidencias.Mapear(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Incidencia incidencia, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incidencia);

        const string sql = """
            INSERT INTO dbo.Incidencias
                (Id, EmpresaId, PeriodoId, ContratoId, DiasPeriodo, Vacaciones, Ausentismos, Incapacidades, Festivos,
                 HorasDobles, HorasTriples, DomingosTrabajados, Gratificacion, Reembolsos, Teletrabajo, Finiquito,
                 Cafeteria, HorasDescontadas, OtrosDescuentos, PrestamoPersonal, Aguinaldo, DescuentosFiscales,
                 FonacotCapturado, DescuentoSindicalAdicional, AjusteSindical, IsrManual, TipoMovimiento,
                 Observaciones, CapturadoPorUsuarioId, FechaCaptura)
            VALUES
                (@Id, @EmpresaId, @PeriodoId, @ContratoId, @DiasPeriodo, @Vacaciones, @Ausentismos, @Incapacidades, @Festivos,
                 @HorasDobles, @HorasTriples, @Domingos, @Gratificacion, @Reembolsos, @Teletrabajo, @Finiquito,
                 @Cafeteria, @HorasDescontadas, @OtrosDescuentos, @PrestamoPersonal, @Aguinaldo, @DescuentosFiscales,
                 @FonacotCapturado, @DescuentoSindical, @AjusteSindical, @IsrManual, @TipoMovimiento,
                 @Observaciones, @Usuario, @Fecha);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, incidencia);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = incidencia.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = incidencia.PeriodoId });
        comando.Parameters.Add(new SqlParameter("@ContratoId", SqlDbType.UniqueIdentifier) { Value = incidencia.ContratoId });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Incidencia incidencia, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incidencia);

        const string sql = """
            UPDATE dbo.Incidencias
            SET    DiasPeriodo = @DiasPeriodo, Vacaciones = @Vacaciones, Ausentismos = @Ausentismos,
                   Incapacidades = @Incapacidades, Festivos = @Festivos, HorasDobles = @HorasDobles,
                   HorasTriples = @HorasTriples, DomingosTrabajados = @Domingos, Gratificacion = @Gratificacion,
                   Reembolsos = @Reembolsos, Teletrabajo = @Teletrabajo, Finiquito = @Finiquito, Cafeteria = @Cafeteria,
                   HorasDescontadas = @HorasDescontadas, OtrosDescuentos = @OtrosDescuentos,
                   PrestamoPersonal = @PrestamoPersonal, Aguinaldo = @Aguinaldo, DescuentosFiscales = @DescuentosFiscales,
                   FonacotCapturado = @FonacotCapturado, DescuentoSindicalAdicional = @DescuentoSindical,
                   AjusteSindical = @AjusteSindical, IsrManual = @IsrManual, TipoMovimiento = @TipoMovimiento,
                   Observaciones = @Observaciones, CapturadoPorUsuarioId = @Usuario, FechaCaptura = @Fecha
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, incidencia);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task EliminarAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearComandoAsync(
            "DELETE FROM dbo.Incidencias WHERE Id = @Id AND EmpresaId = @EmpresaId;", cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AgregarParametros(SqlCommand comando, Incidencia incidencia)
    {
        DatosDeIncidencia d = incidencia.Datos;

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = incidencia.Id });
        comando.Parameters.Add(Dias("@DiasPeriodo", d.DiasPeriodo));
        comando.Parameters.Add(Dias("@Vacaciones", d.Vacaciones));
        comando.Parameters.Add(Dias("@Ausentismos", d.Ausentismos));
        comando.Parameters.Add(Dias("@Incapacidades", d.Incapacidades));
        comando.Parameters.Add(Dias("@Festivos", d.Festivos));
        comando.Parameters.Add(Dias("@HorasDobles", d.HorasDobles));
        comando.Parameters.Add(Dias("@HorasTriples", d.HorasTriples));
        comando.Parameters.Add(Dias("@Domingos", d.DomingosTrabajados));
        comando.Parameters.Add(Importe("@Gratificacion", d.Gratificacion));
        comando.Parameters.Add(Importe("@Reembolsos", d.Reembolsos));
        comando.Parameters.Add(Importe("@Teletrabajo", d.Teletrabajo));
        comando.Parameters.Add(Importe("@Finiquito", d.Finiquito));
        comando.Parameters.Add(Importe("@Cafeteria", d.Cafeteria));
        comando.Parameters.Add(Dias("@HorasDescontadas", d.HorasDescontadas));
        comando.Parameters.Add(Importe("@OtrosDescuentos", d.OtrosDescuentos));
        comando.Parameters.Add(Importe("@PrestamoPersonal", d.PrestamoPersonal));
        comando.Parameters.Add(Importe("@Aguinaldo", d.Aguinaldo));
        comando.Parameters.Add(Importe("@DescuentosFiscales", d.DescuentosFiscales));
        comando.Parameters.Add(Importe("@FonacotCapturado", d.FonacotCapturado));
        comando.Parameters.Add(Importe("@DescuentoSindical", d.DescuentoSindicalAdicional));
        comando.Parameters.Add(Importe("@AjusteSindical", d.AjusteSindical));
        comando.Parameters.Add(Importe("@IsrManual", d.IsrManual));
        comando.Parameters.Add(new SqlParameter("@TipoMovimiento", SqlDbType.TinyInt) { Value = (byte)d.TipoDeMovimiento });
        comando.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 500) { Value = (object?)d.Observaciones ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Usuario", SqlDbType.UniqueIdentifier) { Value = incidencia.CapturadoPorUsuarioId });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = incidencia.FechaCaptura });
    }

    private static SqlParameter Dias(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 9, Scale = 4, Value = valor };

    private static SqlParameter Importe(string nombre, decimal? valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)valor ?? DBNull.Value };
}
