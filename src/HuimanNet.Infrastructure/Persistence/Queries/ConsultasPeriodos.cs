using System.Data;
using System.Globalization;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de períodos: proyecta a <see cref="PeriodoDto"/> incluyendo
/// el recuento de documentos disponibles por dirección del flujo.
/// </summary>
public sealed class ConsultasPeriodos : RepositorioSqlBase, IConsultasPeriodos
{
    /// <summary>
    /// Proyección común: período, empresa y contadores de documentos disponibles.
    /// </summary>
    private const string Proyeccion = """
        SELECT p.Id, p.EmpresaId, e.RazonSocial, p.Anio, p.Mes, p.Consecutivo,
               p.Descripcion, p.Estado, p.FechaApertura, p.FechaLimiteCarga, p.FechaCierre,
               (SELECT COUNT_BIG(1) FROM dbo.Documentos AS dc
                 WHERE dc.PeriodoId = p.Id AND dc.Estado = @EstadoDisponible
                   AND dc.Tipo IN (@Incidencia, @DatosEmpleado)) AS DocumentosCliente,
               (SELECT COUNT_BIG(1) FROM dbo.Documentos AS dr
                 WHERE dr.PeriodoId = p.Id AND dr.Estado = @EstadoDisponible
                   AND dr.Tipo IN (@Resultado, @Ajuste)) AS DocumentosResultado
        FROM   dbo.Periodos AS p
        INNER JOIN dbo.Empresas AS e ON e.Id = p.EmpresaId
        """;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasPeriodos"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasPeriodos(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<PeriodoDto?> ObtenerAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            {Proyeccion}
            WHERE p.Id = @PeriodoId AND p.EmpresaId = @EmpresaId;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametrosDeTipo(comando);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoDto>> ListarPorEmpresaAsync(
        Guid empresaId, bool incluirCerrados, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            {Proyeccion}
            WHERE p.EmpresaId = @EmpresaId
              AND (@IncluirCerrados = 1 OR p.Estado <> @EstadoCerrado)
            ORDER BY p.Anio DESC, p.Mes DESC, p.Consecutivo DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametrosDeTipo(comando);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@IncluirCerrados", SqlDbType.Bit) { Value = incluirCerrados });
        comando.Parameters.Add(new SqlParameter("@EstadoCerrado", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoPeriodo.Cerrado,
        });

        return await LeerVariosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoDto>> ListarBandejaDelOperadorAsync(
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            {Proyeccion}
            WHERE p.Estado IN (@Recibido, @EnProceso)
            ORDER BY p.FechaApertura ASC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametrosDeTipo(comando);
        comando.Parameters.Add(new SqlParameter("@Recibido", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.Recibido });
        comando.Parameters.Add(new SqlParameter("@EnProceso", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.EnProceso });

        return await LeerVariosAsync(comando, cancellationToken);
    }

    private static void AgregarParametrosDeTipo(SqlCommand comando)
    {
        comando.Parameters.Add(new SqlParameter("@EstadoDisponible", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoDocumento.Disponible,
        });
        comando.Parameters.Add(new SqlParameter("@Incidencia", SqlDbType.TinyInt) { Value = (byte)TipoDocumento.Incidencia });
        comando.Parameters.Add(new SqlParameter("@DatosEmpleado", SqlDbType.TinyInt) { Value = (byte)TipoDocumento.DatosEmpleado });
        comando.Parameters.Add(new SqlParameter("@Resultado", SqlDbType.TinyInt) { Value = (byte)TipoDocumento.Resultado });
        comando.Parameters.Add(new SqlParameter("@Ajuste", SqlDbType.TinyInt) { Value = (byte)TipoDocumento.Ajuste });
    }

    private static async Task<IReadOnlyList<PeriodoDto>> LeerVariosAsync(
        SqlCommand comando, CancellationToken cancellationToken)
    {
        var periodos = new List<PeriodoDto>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            periodos.Add(Mapear(reader));
        }

        return periodos;
    }

    private static PeriodoDto Mapear(SqlDataReader reader)
    {
        string clave = string.Create(
            CultureInfo.InvariantCulture,
            $"{reader.GetInt16(3):D4}-{reader.GetByte(4):D2}-{reader.GetByte(5):D2}");

        return new PeriodoDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            clave,
            reader.GetString(6),
            (EstadoPeriodo)reader.GetByte(7),
            reader.GetDateTimeOffset(8),
            reader.IsDBNull(9) ? null : reader.GetDateTimeOffset(9),
            reader.IsDBNull(10) ? null : reader.GetDateTimeOffset(10),
            (int)reader.GetInt64(11),
            (int)reader.GetInt64(12));
    }
}
