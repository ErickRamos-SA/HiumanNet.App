using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.ValueObjects;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de períodos de carga basado en ADO.NET sobre SQL Server.
/// </summary>
public sealed class PeriodoRepository : RepositorioSqlBase, IPeriodoRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PeriodoRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public PeriodoRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<PeriodoCarga?> ObtenerPorIdAsync(
        Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PeriodoCarga?> ObtenerPorCalendarioAsync(
        Guid empresaId, PeriodoCalendario calendario, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.ObtenerPorCalendario, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Anio", SqlDbType.SmallInt) { Value = (short)calendario.Anio });
        comando.Parameters.Add(new SqlParameter("@Mes", SqlDbType.TinyInt) { Value = (byte)calendario.Mes });
        comando.Parameters.Add(new SqlParameter("@Consecutivo", SqlDbType.TinyInt) { Value = (byte)calendario.Consecutivo });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoCarga>> ListarPorEmpresaAsync(
        Guid empresaId, bool incluirCerrados = true, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.ListarPorEmpresa, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@IncluirCerrados", SqlDbType.Bit) { Value = incluirCerrados });
        comando.Parameters.Add(new SqlParameter("@EstadoCerrado", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoPeriodo.Cerrado,
        });

        return await LeerVariosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoCarga>> ListarBandejaDelOperadorAsync(
        CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.ListarBandeja, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Recibido", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.Recibido });
        comando.Parameters.Add(new SqlParameter("@EnProceso", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.EnProceso });

        return await LeerVariosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(PeriodoCarga periodo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.Insertar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = periodo.Id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = periodo.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@Anio", SqlDbType.SmallInt) { Value = (short)periodo.Calendario.Anio });
        comando.Parameters.Add(new SqlParameter("@Mes", SqlDbType.TinyInt) { Value = (byte)periodo.Calendario.Mes });
        comando.Parameters.Add(new SqlParameter("@Consecutivo", SqlDbType.TinyInt) { Value = (byte)periodo.Calendario.Consecutivo });
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 200) { Value = periodo.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)periodo.Estado });
        comando.Parameters.Add(new SqlParameter("@FechaApertura", SqlDbType.DateTimeOffset) { Value = periodo.FechaApertura });
        comando.Parameters.Add(FechaOpcional("@FechaLimiteCarga", periodo.FechaLimiteCarga));
        comando.Parameters.Add(FechaOpcional("@FechaCierre", periodo.FechaCierre));

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(PeriodoCarga periodo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.Actualizar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = periodo.Id });
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 200) { Value = periodo.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)periodo.Estado });
        comando.Parameters.Add(FechaOpcional("@FechaLimiteCarga", periodo.FechaLimiteCarga));
        comando.Parameters.Add(FechaOpcional("@FechaCierre", periodo.FechaCierre));

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Crea un parámetro de fecha opcional.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Fecha, o <c>null</c>.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter FechaOpcional(string nombre, DateTimeOffset? valor)
        => new(nombre, SqlDbType.DateTimeOffset) { Value = (object?)valor ?? DBNull.Value };

    /// <summary>Ejecuta un procedimiento y rehidrata la primera fila.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El período, o <c>null</c> si no hay filas.</returns>
    private static async Task<PeriodoCarga?> LeerUnoAsync(
        SqlCommand comando, CancellationToken cancellationToken)
    {
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? LectorDePeriodos.Mapear(reader)
            : null;
    }

    /// <summary>Ejecuta un procedimiento y rehidrata cada fila.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los períodos.</returns>
    private static async Task<IReadOnlyList<PeriodoCarga>> LeerVariosAsync(
        SqlCommand comando, CancellationToken cancellationToken)
    {
        var periodos = new List<PeriodoCarga>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            periodos.Add(LectorDePeriodos.Mapear(reader));
        }

        return periodos;
    }
}
