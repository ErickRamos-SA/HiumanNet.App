using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Serializacion;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de corridas de nómina y de sus resultados.
/// </summary>
/// <remarks>
/// Los resultados se insertan con <see cref="SqlBulkCopy"/> dentro de la
/// transacción del caso de uso, no con un procedimiento: una corrida de miles
/// de trabajadores se persiste en un solo viaje de datos en lugar de miles de
/// llamadas. Todo lo demás pasa por los procedimientos de
/// <c>Procedimientos/Nomina.sql</c>.
/// </remarks>
public sealed class CorridaDeNominaRepository : RepositorioSqlBase, ICorridaDeNominaRepository
{
    private static readonly ColumnaEnMemoria<ResultadoDeNomina>[] ColumnasDeResultado =
    [
        new("Id", typeof(Guid), static r => r.Id),
        new("CorridaId", typeof(Guid), static r => r.CorridaId),
        new("EmpresaId", typeof(Guid), static r => r.EmpresaId),
        new("ContratoId", typeof(Guid), static r => r.ContratoId),
        new("EmpleadoId", typeof(Guid), static r => r.EmpleadoId),
        new("RazonSocialId", typeof(Guid), static r => r.RazonSocialId),
        new("Esquema", typeof(byte), static r => (byte)r.Esquema),
        new("ClaveEmpleado", typeof(string), static r => r.ClaveEmpleado),
        new("NombreEmpleado", typeof(string), static r => r.NombreEmpleado),
        new("TipoMovimiento", typeof(byte), static r => (byte)r.TipoDeMovimiento),
        new("Bruto", typeof(decimal), static r => Redondear(r.Resumen.Bruto)),
        new("TotalPercepciones", typeof(decimal), static r => Redondear(r.Resumen.TotalPercepciones)),
        new("TotalDeducciones", typeof(decimal), static r => Redondear(r.Resumen.TotalDeducciones)),
        new("Neto", typeof(decimal), static r => Redondear(r.Resumen.Neto)),
        new("Isr", typeof(decimal), static r => Redondear(r.Resumen.Isr)),
        new("Subsidio", typeof(decimal), static r => Redondear(r.Resumen.Subsidio)),
        new("ImssTrabajador", typeof(decimal), static r => Redondear(r.Resumen.ImssTrabajador)),
        new("ImssPatronal", typeof(decimal), static r => Redondear(r.Resumen.ImssPatronal)),
        new("InfonavitPatronal", typeof(decimal), static r => Redondear(r.Resumen.InfonavitPatronal)),
        new("InfonavitTrabajador", typeof(decimal), static r => Redondear(r.Resumen.InfonavitTrabajador)),
        new("Fonacot", typeof(decimal), static r => Redondear(r.Resumen.Fonacot)),
        new("Isn", typeof(decimal), static r => Redondear(r.Resumen.Isn)),
        new("ComplementoSindical", typeof(decimal), static r => Redondear(r.Resumen.ComplementoSindical)),
        new("Facturable", typeof(decimal), static r => Redondear(r.Resumen.Facturable)),
        new("Comision", typeof(decimal), static r => Redondear(r.Resumen.Comision)),
        new("CostoTotal", typeof(decimal), static r => Redondear(r.Resumen.CostoTotal)),
        new("CostoIsr", typeof(decimal), static r => Redondear(r.Resumen.CostoIsr)),
        new("CostoImss", typeof(decimal), static r => Redondear(r.Resumen.CostoImss)),
        new("CostoInfonavit", typeof(decimal), static r => Redondear(r.Resumen.CostoInfonavit)),
        new("CostoOtros", typeof(decimal), static r => Redondear(r.Resumen.CostoOtros)),
        new("Advertencia", typeof(string), static r => r.Advertencia is null ? null : Recortar(r.Advertencia, 1000)),
        new("Conceptos", typeof(string), static r => SerializadorDeNomina.SerializarConceptos(r.Conceptos)),
    ];

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CorridaDeNominaRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public CorridaDeNominaRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<CorridaDeNomina?> ObtenerAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Nomina.CorridaObtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCorridas.MapearCorrida(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CorridaDeNomina>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridasListarPorPeriodo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        var lista = new List<CorridaDeNomina>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeCorridas.MapearCorrida(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<int> SiguienteNumeroAsync(Guid periodoId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridaSiguienteNumero, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });

        object? valor = await comando.ExecuteScalarAsync(cancellationToken);
        return valor is int numero ? numero : 1;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(
        CorridaDeNomina corrida, IReadOnlyList<ResultadoDeNomina> resultados, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(corrida);
        ArgumentNullException.ThrowIfNull(resultados);

        TotalesDeCorrida t = corrida.Totales;

        await using (SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Nomina.CorridaInsertar, cancellationToken))
        {
            comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = corrida.Id });
            comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = corrida.EmpresaId });
            comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = corrida.PeriodoId });
            comando.Parameters.Add(new SqlParameter("@Numero", SqlDbType.Int) { Value = corrida.Numero });
            comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)corrida.Estado });
            comando.Parameters.Add(new SqlParameter("@FechaReferencia", SqlDbType.Date) { Value = corrida.FechaDeReferencia });
            comando.Parameters.Add(new SqlParameter("@FechaCalculo", SqlDbType.DateTimeOffset) { Value = corrida.FechaCalculo });
            comando.Parameters.Add(new SqlParameter("@Usuario", SqlDbType.UniqueIdentifier) { Value = corrida.CalculadaPorUsuarioId });
            comando.Parameters.Add(new SqlParameter("@Trabajadores", SqlDbType.Int) { Value = t.Trabajadores });
            comando.Parameters.Add(Importe("@Bruto", t.Bruto));
            comando.Parameters.Add(Importe("@Percepciones", t.Percepciones));
            comando.Parameters.Add(Importe("@Deducciones", t.Deducciones));
            comando.Parameters.Add(Importe("@Neto", t.Neto));
            comando.Parameters.Add(Importe("@Isr", t.Isr));
            comando.Parameters.Add(Importe("@ImssTrabajador", t.ImssTrabajador));
            comando.Parameters.Add(Importe("@ImssPatronal", t.ImssPatronal));
            comando.Parameters.Add(Importe("@Infonavit", t.Infonavit));
            comando.Parameters.Add(Importe("@Isn", t.Isn));
            comando.Parameters.Add(Importe("@Complemento", t.ComplementoSindical));
            comando.Parameters.Add(Importe("@Facturable", t.Facturable));
            comando.Parameters.Add(Importe("@Comision", t.Comision));
            comando.Parameters.Add(Importe("@Costo", t.CostoTotal));
            comando.Parameters.Add(new SqlParameter("@Duracion", SqlDbType.BigInt) { Value = corrida.DuracionMs });
            comando.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 1000) { Value = (object?)corrida.Observaciones ?? DBNull.Value });
            comando.Parameters.Add(new SqlParameter("@Advertencias", SqlDbType.NVarChar, -1) { Value = (object?)corrida.Advertencias ?? DBNull.Value });
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        if (resultados.Count == 0)
        {
            return;
        }

        SqlConnection conexion = await Sesion.ObtenerConexionAsync(cancellationToken);

        using var copia = new SqlBulkCopy(conexion, SqlBulkCopyOptions.CheckConstraints, Sesion.TransaccionActual)
        {
            DestinationTableName = "dbo.ResultadosDeNomina",
            BatchSize = 2_000,
            BulkCopyTimeout = Math.Max(Sesion.TiempoDeEsperaComandoSegundos, 120),
            EnableStreaming = true,
        };

        foreach (ColumnaEnMemoria<ResultadoDeNomina> columna in ColumnasDeResultado)
        {
            copia.ColumnMappings.Add(columna.Nombre, columna.Nombre);
        }

        await using var lector = new LectorEnMemoria<ResultadoDeNomina>(resultados, ColumnasDeResultado);
        await copia.WriteToServerAsync(lector, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(CorridaDeNomina corrida, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(corrida);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.CorridaActualizar, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = corrida.Id });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)corrida.Estado });
        comando.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 1000) { Value = (object?)corrida.Observaciones ?? DBNull.Value });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResultadoDeNomina>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.ResultadosListarPorCorrida, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        var lista = new List<ResultadoDeNomina>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(Mapear(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<ResultadoDeNomina?> ObtenerResultadoAsync(
        Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Nomina.ResultadoObtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = resultadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Mapear(reader) : null;
    }

    /// <summary>
    /// Lee un resultado en acceso secuencial: las columnas se consumen en orden
    /// y la columna de conceptos, la más grande, se lee al final.
    /// </summary>
    /// <param name="reader">Lector situado en la fila.</param>
    /// <returns>El resultado rehidratado.</returns>
    private static ResultadoDeNomina Mapear(SqlDataReader reader)
    {
        var valores = new object[32];
        reader.GetValues(valores);

        return ResultadoDeNomina.Rehidratar(
            (Guid)valores[0], (Guid)valores[1], (Guid)valores[2], (Guid)valores[3], (Guid)valores[4], (Guid)valores[5],
            (Domain.Enums.EsquemaDePago)(byte)valores[6], (string)valores[7], (string)valores[8],
            (Domain.Enums.TipoDeMovimiento)(byte)valores[9],
            new ResumenDeResultado(
                D(valores[10]), D(valores[11]), D(valores[12]), D(valores[13]), D(valores[14]), D(valores[15]),
                D(valores[16]), D(valores[17]), D(valores[18]), D(valores[19]), D(valores[20]), D(valores[21]),
                D(valores[22]), D(valores[23]), D(valores[24]), D(valores[25]), D(valores[26]), D(valores[27]),
                D(valores[28]), D(valores[29])),
            SerializadorDeNomina.LeerConceptos(valores[31] as string),
            valores[30] as string);
    }

    /// <summary>Convierte una columna leída a importe.</summary>
    /// <param name="valor">Valor de la columna.</param>
    /// <returns>El importe.</returns>
    private static decimal D(object valor) => (decimal)valor;

    /// <summary>Redondea un importe a los cuatro decimales de la columna.</summary>
    /// <param name="valor">Importe calculado.</param>
    /// <returns>El importe redondeado, con los medios lejos del cero.</returns>
    private static decimal Redondear(decimal valor) => Math.Round(valor, 4, MidpointRounding.AwayFromZero);

    /// <summary>Recorta un texto a la longitud de su columna.</summary>
    /// <param name="valor">Texto.</param>
    /// <param name="maximo">Longitud de la columna.</param>
    /// <returns>El texto, recortado si excede la longitud.</returns>
    private static string Recortar(string valor, int maximo) => valor.Length <= maximo ? valor : valor[..maximo];

    /// <summary>Crea un parámetro de importe redondeado a cuatro decimales.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Importe.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Importe(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = Redondear(valor) };
}
