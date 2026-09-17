using System.Data;
using System.Globalization;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de empleados y contratos.
/// </summary>
/// <remarks>
/// El listado se pagina en SQL Server (<c>OFFSET ... FETCH</c>) y resuelve en la
/// misma sentencia los contratos vigentes de cada empleado, para que la
/// pantalla de una empresa con miles de trabajadores responda en un solo viaje.
/// </remarks>
public sealed class ConsultasEmpleados : RepositorioSqlBase, IConsultasEmpleados
{
    /// <summary>Columna del parámetro de tabla con el que viajan las empresas del ámbito.</summary>
    private static readonly SqlMetaData[] ColumnasDeIdentificador =
    [
        new("Id", SqlDbType.UniqueIdentifier),
    ];

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasEmpleados"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasEmpleados(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<PaginaDto<EmpleadoResumenDto>> ListarAsync(
        IReadOnlyCollection<Guid>? empresas, bool soloActivos, string? texto, int pagina, int tamanoPagina, CancellationToken cancellationToken = default)
    {
        // Sin empresas (roles transversales) se listan todas; si no, sólo las
        // indicadas, que viajan en un parámetro de tabla.
        IReadOnlyList<Guid> ambito = empresas is null ? [] : [.. empresas];

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ListarPaginado, cancellationToken);

        comando.Parameters.Add(ParametrosDeTabla.Crear(
            "@Empresas", "dbo.IdentificadoresTipo", ColumnasDeIdentificador, ambito,
            static (registro, id) => registro.SetGuid(0, id)));
        comando.Parameters.Add(new SqlParameter("@FiltrarEmpresas", SqlDbType.Bit) { Value = empresas is not null });
        comando.Parameters.Add(new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos });
        comando.Parameters.Add(new SqlParameter("@Texto", SqlDbType.NVarChar, 210)
        {
            Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : "%" + EscaparLike(texto.Trim()) + "%",
        });
        comando.Parameters.Add(new SqlParameter("@Omitir", SqlDbType.Int) { Value = (pagina - 1) * tamanoPagina });
        comando.Parameters.Add(new SqlParameter("@Tomar", SqlDbType.Int) { Value = tamanoPagina });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        int total = await reader.ReadAsync(cancellationToken) ? (int)reader.GetInt64(0) : 0;
        var elementos = new List<EmpleadoResumenDto>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                string esquemas = string.Join(
                    ",",
                    reader.GetString(5).Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(static e => ((EsquemaDePago)byte.Parse(e, CultureInfo.InvariantCulture)).ToString())
                        .Distinct());

                elementos.Add(new EmpleadoResumenDto(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
                    (int)reader.GetInt64(4), esquemas, reader.GetString(6), reader.GetGuid(7), reader.GetString(8)));
            }
        }

        int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        return new PaginaDto<EmpleadoResumenDto>(elementos, total, pagina, tamanoPagina, totalPaginas);
    }

    /// <inheritdoc/>
    public async Task<EmpleadoDto?> ObtenerAsync(Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ObtenerDetalle, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empleadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        Empleado empleado = LectorDeEmpleados.Mapear(reader);
        var contratos = new List<ContratoDto>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                contratos.Add(Mapeadores.ADto(LectorDeContratos.Mapear(reader), reader.GetString(26)));
            }
        }

        return Mapeadores.ADto(empleado, contratos);
    }

    /// <summary>Escapa los comodines de <c>LIKE</c> para buscar el texto literal.</summary>
    /// <param name="texto">Texto escrito por el usuario.</param>
    /// <returns>El texto con <c>[</c>, <c>%</c> y <c>_</c> entre corchetes.</returns>
    private static string EscaparLike(string texto)
        => texto.Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
}
