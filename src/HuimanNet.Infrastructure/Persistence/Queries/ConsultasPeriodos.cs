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
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Periodos.ObtenerDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        AgregarParametrosDeTipo(comando);

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoDto>> ListarPorEmpresaAsync(
        Guid empresaId, bool incluirCerrados, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Periodos.ListarDetallePorEmpresa, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@IncluirCerrados", SqlDbType.Bit) { Value = incluirCerrados });
        comando.Parameters.Add(new SqlParameter("@EstadoCerrado", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoPeriodo.Cerrado,
        });
        AgregarParametrosDeTipo(comando);

        return await LeerVariosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PeriodoDto>> ListarBandejaDelOperadorAsync(
        CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Periodos.ListarDetalleBandeja, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Recibido", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.Recibido });
        comando.Parameters.Add(new SqlParameter("@EnProceso", SqlDbType.TinyInt) { Value = (byte)EstadoPeriodo.EnProceso });
        AgregarParametrosDeTipo(comando);

        return await LeerVariosAsync(comando, cancellationToken);
    }

    /// <summary>
    /// Agrega los parámetros con los que el procedimiento cuenta los documentos
    /// disponibles del cliente y de resultados.
    /// </summary>
    /// <param name="comando">Comando de la consulta.</param>
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

    /// <summary>Ejecuta un procedimiento de períodos y proyecta cada fila.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los períodos.</returns>
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

    /// <summary>Proyecta una fila a su DTO, con la clave <c>AAAA-MM-NN</c> del período.</summary>
    /// <param name="reader">Lector situado en la fila.</param>
    /// <returns>El período.</returns>
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
