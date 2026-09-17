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
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Incidencias.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeIncidencias.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<Incidencia?> ObtenerPorContratoAsync(
        Guid periodoId, Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Incidencias.ObtenerPorContrato, cancellationToken);
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
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Incidencias.ListarPorPeriodo, cancellationToken);
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

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Incidencias.Insertar, cancellationToken);
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

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Incidencias.Actualizar, cancellationToken);
        AgregarParametros(comando, incidencia);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task EliminarAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Incidencias.Eliminar, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Agrega los valores de una incidencia al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="incidencia">Incidencia.</param>
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

    /// <summary>Crea un parámetro de días u horas con cuatro decimales.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Cantidad.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Dias(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 9, Scale = 4, Value = valor };

    /// <summary>Crea un parámetro de importe opcional con cuatro decimales.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Importe, o <c>null</c>.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Importe(string nombre, decimal? valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)valor ?? DBNull.Value };
}
