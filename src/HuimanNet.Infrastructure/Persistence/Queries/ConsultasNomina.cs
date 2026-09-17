using System.Data;
using System.Globalization;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de corridas, resultados y cotejos.
/// </summary>
/// <remarks>
/// Los listados no leen la columna de conceptos: el detalle de un trabajador
/// sólo se carga cuando se pide, lo que mantiene ligera la pantalla de una
/// corrida con miles de resultados.
/// </remarks>
public sealed class ConsultasNomina : RepositorioSqlBase, IConsultasNomina
{
    /// <summary>Corridas recientes que puede pedir el panel de inicio.</summary>
    private const int LimiteMaximoDeRecientes = 200;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasNomina"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasNomina(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CorridaDeNominaDto>> ListarCorridasAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridasListarDetallePorPeriodo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerCorridasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CorridaDeNominaDto>> ListarRecientesAsync(
        Guid? empresaId, int limite, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridasListarDetalleRecientes, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.Int) { Value = Math.Clamp(limite, 1, LimiteMaximoDeRecientes) });

        return await LeerCorridasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CorridaDeNominaDto?> ObtenerCorridaAsync(Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridaObtenerDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        IReadOnlyList<CorridaDeNominaDto> lista = await LeerCorridasAsync(comando, cancellationToken);
        return lista.Count == 0 ? null : lista[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResultadoDeNominaDto>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.ResultadosListarDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerResultadosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ResultadoDeNominaDto?> ObtenerResultadoAsync(Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.ResultadoObtenerDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = resultadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        IReadOnlyList<ResultadoDeNominaDto> lista = await LeerResultadosAsync(comando, cancellationToken);
        return lista.Count == 0 ? null : lista[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CotejoDto>> ListarCotejosAsync(Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CotejosListarDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        var lista = new List<CotejoDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(Mapeadores.ADto(LectorDeCorridas.MapearCotejo(reader, []), reader.GetString(9), incluirDetalle: false));
        }

        return lista;
    }

    /// <summary>Ejecuta un procedimiento de corridas y proyecta cada fila a su DTO.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas, con la clave de su período.</returns>
    private static async Task<IReadOnlyList<CorridaDeNominaDto>> LeerCorridasAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        var lista = new List<CorridaDeNominaDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            CorridaDeNomina corrida = LectorDeCorridas.MapearCorrida(reader);
            string clave = string.Create(
                CultureInfo.InvariantCulture, $"{reader.GetInt16(25):D4}-{reader.GetByte(26):D2}-{reader.GetByte(27):D2}");

            lista.Add(Mapeadores.ADto(corrida, clave, reader.GetString(28), reader.GetString(29)));
        }

        return lista;
    }

    /// <summary>
    /// Ejecuta un procedimiento de resultados y proyecta cada fila a su DTO, sin
    /// el detalle de conceptos.
    /// </summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los resultados.</returns>
    private static async Task<IReadOnlyList<ResultadoDeNominaDto>> LeerResultadosAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        var lista = new List<ResultadoDeNominaDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(Mapeadores.ADto(LectorDeCorridas.MapearResultado(reader, []), reader.GetString(31)));
        }

        return lista;
    }
}
