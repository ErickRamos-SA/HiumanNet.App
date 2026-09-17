using System.Data;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de incidencias: una fila por contrato vigente, con la
/// incidencia capturada o las cantidades de un período completo.
/// </summary>
public sealed class ConsultasIncidencias : RepositorioSqlBase, IConsultasIncidencias
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasIncidencias"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasIncidencias(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IncidenciaDto>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, DateOnly fechaDeReferencia, decimal diasPeriodoPredeterminados,
        CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Incidencias.ListarDetallePorPeriodo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.Date) { Value = fechaDeReferencia });
        comando.Parameters.Add(new SqlParameter("@InicioDeMes", SqlDbType.Date)
        {
            Value = new DateOnly(fechaDeReferencia.Year, fechaDeReferencia.Month, 1),
        });

        var lista = new List<IncidenciaDto>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            bool capturada = !reader.IsDBNull(0);
            DatosDeIncidencia d = capturada
                ? LectorDeIncidencias.MapearDatos(reader, 7)
                : DatosDeIncidencia.SinNovedades(diasPeriodoPredeterminados);

            lista.Add(new IncidenciaDto(
                capturada ? reader.GetGuid(0) : null,
                periodoId,
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                (EsquemaDePago)reader.GetByte(6),
                d.DiasPeriodo, d.Vacaciones, d.Ausentismos, d.Incapacidades, d.Festivos, d.HorasDobles, d.HorasTriples,
                d.DomingosTrabajados, d.Gratificacion, d.Reembolsos, d.Teletrabajo, d.Finiquito, d.Cafeteria,
                d.HorasDescontadas, d.OtrosDescuentos, d.PrestamoPersonal, d.Aguinaldo, d.DescuentosFiscales,
                d.FonacotCapturado, d.DescuentoSindicalAdicional, d.AjusteSindical, d.IsrManual, d.TipoDeMovimiento,
                d.Observaciones,
                reader.IsDBNull(31) ? null : reader.GetString(31),
                reader.IsDBNull(32) ? null : reader.GetDateTimeOffset(32)));
        }

        return lista;
    }
}
