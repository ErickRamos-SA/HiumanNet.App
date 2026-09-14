using System.Data;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

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
        // Sin empresas (roles transversales) se listan todas; si no, las
        // indicadas, que llegan separadas por comas. OPTION (RECOMPILE) impide
        // que el plan de «todas» se reutilice al filtrar por pocas empresas, que
        // así aprovechan el índice por EmpresaId.
        const string filtro = """
            (@Empresas IS NULL OR e.EmpresaId IN (SELECT TRY_CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@Empresas, ',')))
              AND (@SoloActivos = 0 OR e.Activo = 1)
              AND (@Texto IS NULL OR e.Clave LIKE @Texto
                   OR (e.Nombre + N' ' + e.ApellidoPaterno + ISNULL(N' ' + e.ApellidoMaterno, N'')) LIKE @Texto)
            """;

        string sql = $"""
            SELECT COUNT_BIG(1) FROM dbo.Empleados AS e WHERE {filtro} OPTION (RECOMPILE);

            SELECT e.Id, e.Clave, e.Nombre + N' ' + e.ApellidoPaterno + ISNULL(N' ' + e.ApellidoMaterno, N''), e.Activo,
                   (SELECT COUNT_BIG(1) FROM dbo.Contratos AS c WHERE c.EmpleadoId = e.Id AND c.FechaBaja IS NULL),
                   ISNULL((SELECT STRING_AGG(CAST(c.Esquema AS NVARCHAR(3)), N',') FROM dbo.Contratos AS c
                           WHERE c.EmpleadoId = e.Id AND c.FechaBaja IS NULL), N''),
                   ISNULL((SELECT STRING_AGG(r.Nombre, N', ') FROM dbo.Contratos AS c
                           INNER JOIN dbo.RazonesSociales AS r ON r.Id = c.RazonSocialId
                           WHERE c.EmpleadoId = e.Id AND c.FechaBaja IS NULL), N''),
                   e.EmpresaId, emp.RazonSocial
            FROM   dbo.Empleados AS e
            INNER JOIN dbo.Empresas AS emp ON emp.Id = e.EmpresaId
            WHERE  {filtro}
            ORDER BY emp.RazonSocial, LEN(e.Clave), e.Clave
            OFFSET @Omitir ROWS FETCH NEXT @Tomar ROWS ONLY
            OPTION (RECOMPILE);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Empresas", SqlDbType.NVarChar, -1)
        {
            Value = empresas is null ? DBNull.Value : string.Join(",", empresas),
        });
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
                        .Select(static e => ((EsquemaDePago)byte.Parse(e, System.Globalization.CultureInfo.InvariantCulture)).ToString())
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
        string sql = $"""
            SELECT {LectorDeEmpleados.Columnas} FROM dbo.Empleados AS e WHERE e.Id = @Id AND e.EmpresaId = @EmpresaId;

            SELECT {LectorDeContratos.Columnas}, r.Nombre
            FROM   dbo.Contratos AS c
            INNER JOIN dbo.RazonesSociales AS r ON r.Id = c.RazonSocialId
            WHERE  c.EmpleadoId = @Id AND c.EmpresaId = @EmpresaId
            ORDER BY c.FechaBaja, c.FechaAlta DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
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

    private static string EscaparLike(string texto)
        => texto.Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
}

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
        const string sql = """
            SELECT i.Id, c.Id, e.Id, e.Clave, e.Nombre + N' ' + e.ApellidoPaterno + ISNULL(N' ' + e.ApellidoMaterno, N''),
                   r.Nombre, c.Esquema,
                   i.DiasPeriodo, i.Vacaciones, i.Ausentismos, i.Incapacidades, i.Festivos, i.HorasDobles, i.HorasTriples,
                   i.DomingosTrabajados, i.Gratificacion, i.Reembolsos, i.Teletrabajo, i.Finiquito, i.Cafeteria,
                   i.HorasDescontadas, i.OtrosDescuentos, i.PrestamoPersonal, i.Aguinaldo, i.DescuentosFiscales,
                   i.FonacotCapturado, i.DescuentoSindicalAdicional, i.AjusteSindical, i.IsrManual, i.TipoMovimiento,
                   i.Observaciones, u.NombreCompleto, i.FechaCaptura
            FROM   dbo.Contratos AS c
            INNER JOIN dbo.Empleados AS e ON e.Id = c.EmpleadoId AND e.Activo = 1
            INNER JOIN dbo.RazonesSociales AS r ON r.Id = c.RazonSocialId
            LEFT JOIN dbo.Incidencias AS i ON i.ContratoId = c.Id AND i.PeriodoId = @PeriodoId
            LEFT JOIN dbo.Usuarios AS u ON u.Id = i.CapturadoPorUsuarioId
            WHERE  c.EmpresaId = @EmpresaId
              AND  c.FechaAlta <= @Fecha
              AND  (c.FechaBaja IS NULL OR c.FechaBaja >= @InicioDeMes)
            ORDER BY LEN(e.Clave), e.Clave, c.Esquema;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
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
