using System.Data;
using HuimanNet.Application.Inicio;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de los datos del panel de inicio.
/// </summary>
/// <remarks>
/// Los indicadores, los períodos y las corridas se obtienen en un único viaje
/// a la base de datos con tres conjuntos de resultados. Qué pendientes se
/// sugieren lo decide la capa de aplicación.
/// </remarks>
public sealed class ConsultasInicio : RepositorioSqlBase, IConsultasInicio
{
    /// <summary>Períodos sin cerrar que se muestran en el panel.</summary>
    private const int MaximoDePeriodos = 8;

    /// <summary>Corridas recientes que se muestran en el panel.</summary>
    private const int MaximoDeCorridas = 5;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasInicio"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasInicio(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<DatosDeInicio> ObtenerDatosAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Inicio.ObtenerDatos, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)empresaId ?? DBNull.Value,
        });
        comando.Parameters.Add(Estado("@EstadoDisponible", (byte)EstadoDocumento.Disponible));
        comando.Parameters.Add(Estado("@EstadoCerrado", (byte)EstadoPeriodo.Cerrado));
        comando.Parameters.Add(Estado("@EstadoAbierto", (byte)EstadoPeriodo.Abierto));
        comando.Parameters.Add(Estado("@EstadoRecibido", (byte)EstadoPeriodo.Recibido));
        comando.Parameters.Add(Estado("@CorridaCalculada", (byte)EstadoDeCorrida.Calculada));
        comando.Parameters.Add(Estado("@Incidencia", (byte)TipoDocumento.Incidencia));
        comando.Parameters.Add(Estado("@DatosEmpleado", (byte)TipoDocumento.DatosEmpleado));
        comando.Parameters.Add(Estado("@Resultado", (byte)TipoDocumento.Resultado));
        comando.Parameters.Add(Estado("@Ajuste", (byte)TipoDocumento.Ajuste));
        comando.Parameters.Add(new SqlParameter("@MaximoPeriodos", SqlDbType.Int) { Value = MaximoDePeriodos });
        comando.Parameters.Add(new SqlParameter("@MaximoCorridas", SqlDbType.Int) { Value = MaximoDeCorridas });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        int empleados = 0, periodos = 0, documentos = 0, corridas = 0;

        if (await reader.ReadAsync(cancellationToken))
        {
            empleados = (int)reader.GetInt64(0);
            periodos = (int)reader.GetInt64(1);
            documentos = (int)reader.GetInt64(2);
            corridas = (int)reader.GetInt64(3);
        }

        var listaDePeriodos = new List<PeriodoDeInicio>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                listaDePeriodos.Add(new PeriodoDeInicio(
                    reader.GetGuid(0), reader.GetGuid(4), (EstadoPeriodo)reader.GetByte(1), reader.GetString(2), reader.GetString(3),
                    (int)reader.GetInt64(5), (int)reader.GetInt64(6)));
            }
        }

        var listaDeCorridas = new List<CorridaDeInicio>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                listaDeCorridas.Add(new CorridaDeInicio(
                    reader.GetGuid(0), reader.GetGuid(3), reader.GetInt32(1), reader.GetString(2)));
            }
        }

        return new DatosDeInicio(empleados, periodos, documentos, corridas, listaDePeriodos, listaDeCorridas);
    }

    /// <summary>Crea un parámetro con el valor de un enumerado del dominio.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Valor del enumerado.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Estado(string nombre, byte valor)
        => new(nombre, SqlDbType.TinyInt) { Value = valor };
}
