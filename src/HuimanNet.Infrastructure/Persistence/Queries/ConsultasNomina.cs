using System.Data;
using System.Globalization;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Services;
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
    private static readonly string ProyeccionDeCorrida = $"""
        SELECT {LectorDeCorridas.ColumnasDeCorrida}, p.Anio, p.Mes, p.Consecutivo, p.Descripcion,
               ISNULL(u.NombreCompleto, N'')
        FROM   dbo.CorridasDeNomina AS k
        INNER JOIN dbo.Periodos AS p ON p.Id = k.PeriodoId
        LEFT JOIN dbo.Usuarios AS u ON u.Id = k.CalculadaPorUsuarioId
        """;

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
        string sql = $"{ProyeccionDeCorrida} WHERE k.PeriodoId = @PeriodoId AND k.EmpresaId = @EmpresaId ORDER BY k.Numero DESC;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerCorridasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CorridaDeNominaDto>> ListarRecientesAsync(
        Guid? empresaId, int limite, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            {ProyeccionDeCorrida.Replace("SELECT ", "SELECT TOP (@Limite) ", StringComparison.Ordinal)}
            WHERE (@EmpresaId IS NULL OR k.EmpresaId = @EmpresaId)
            ORDER BY k.FechaCalculo DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.Int) { Value = Math.Clamp(limite, 1, 200) });

        return await LeerCorridasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CorridaDeNominaDto?> ObtenerCorridaAsync(Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"{ProyeccionDeCorrida} WHERE k.Id = @Id AND k.EmpresaId = @EmpresaId;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        IReadOnlyList<CorridaDeNominaDto> lista = await LeerCorridasAsync(comando, cancellationToken);
        return lista.Count == 0 ? null : lista[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResultadoDeNominaDto>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCorridas.ColumnasDeResultado}, rs.Nombre
            FROM   dbo.ResultadosDeNomina AS n
            INNER JOIN dbo.RazonesSociales AS rs ON rs.Id = n.RazonSocialId
            WHERE  n.CorridaId = @CorridaId AND n.EmpresaId = @EmpresaId
            ORDER BY rs.Nombre, LEN(n.ClaveEmpleado), n.ClaveEmpleado, n.Esquema;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@CorridaId", SqlDbType.UniqueIdentifier) { Value = corridaId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerResultadosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ResultadoDeNominaDto?> ObtenerResultadoAsync(Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCorridas.ColumnasDeResultado}, rs.Nombre
            FROM   dbo.ResultadosDeNomina AS n
            INNER JOIN dbo.RazonesSociales AS rs ON rs.Id = n.RazonSocialId
            WHERE  n.Id = @Id AND n.EmpresaId = @EmpresaId;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = resultadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        IReadOnlyList<ResultadoDeNominaDto> lista = await LeerResultadosAsync(comando, cancellationToken);
        return lista.Count == 0 ? null : lista[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CotejoDto>> ListarCotejosAsync(Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCorridas.ColumnasDeCotejo}, ISNULL(u.NombreCompleto, N'')
            FROM   dbo.CotejosDeNomina AS j
            LEFT JOIN dbo.Usuarios AS u ON u.Id = j.UsuarioId
            WHERE  j.CorridaId = @CorridaId AND j.EmpresaId = @EmpresaId
            ORDER BY j.FechaCotejo DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
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

/// <summary>
/// Lado de lectura de usuarios para la administración.
/// </summary>
public sealed class ConsultasUsuarios : RepositorioSqlBase, IConsultasUsuarios
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasUsuarios"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasUsuarios(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        // Un usuario de empresa cliente aparece en el filtro de cualquiera de
        // sus empresas, principal o adicional.
        const string filtro = """
            u.Id <> '00000000-0000-0000-0000-000000000001'
              AND (@EmpresaId IS NULL OR u.EmpresaId = @EmpresaId
                   OR EXISTS (SELECT 1 FROM dbo.EmpresasAdicionalesDeUsuario AS a WHERE a.UsuarioId = u.Id AND a.EmpresaId = @EmpresaId))
              AND (@IncluirInactivos = 1 OR u.Activo = 1)
            """;

        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas} FROM dbo.Usuarios AS u WHERE {filtro} ORDER BY u.NombreCompleto;
            SELECT {LectorDeUsuarios.ColumnasDePermiso} FROM dbo.PermisosDeUsuario AS p
            INNER JOIN dbo.Usuarios AS u ON u.Id = p.UsuarioId WHERE {filtro};
            SELECT {LectorDeUsuarios.ColumnasDeEmpresaAdicional} FROM dbo.EmpresasAdicionalesDeUsuario AS x
            INNER JOIN dbo.Usuarios AS u ON u.Id = x.UsuarioId WHERE {filtro};
            SELECT e.Id, e.RazonSocial FROM dbo.Empresas AS e
            WHERE e.Id IN (SELECT u.EmpresaId FROM dbo.Usuarios AS u WHERE {filtro});
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@IncluirInactivos", SqlDbType.Bit) { Value = incluirInactivos });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        IReadOnlyList<Usuario> usuarios = await LectorDeUsuarios.LeerAsync(reader, cancellationToken);

        var empresas = new Dictionary<Guid, string>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                empresas[reader.GetGuid(0)] = reader.GetString(1);
            }
        }

        return usuarios
            .Select(u => Mapeadores.ADto(
                u,
                u.EmpresaId is { } id && empresas.TryGetValue(id, out string? nombre) ? nombre : null,
                PermisosPorRol.Efectivas(u.Rol, u.Permisos)))
            .ToList();
    }
}

/// <summary>
/// Lado de lectura de los indicadores del panel de inicio.
/// </summary>
/// <remarks>
/// Todos los indicadores y las tareas sugeridas se obtienen en un único viaje
/// a la base de datos con tres conjuntos de resultados.
/// </remarks>
public sealed class ConsultasInicio : RepositorioSqlBase, IConsultasInicio
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasInicio"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasInicio(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<ResumenDeInicioDto> ObtenerResumenAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        byte disponible = (byte)EstadoDocumento.Disponible;
        byte cerrado = (byte)EstadoPeriodo.Cerrado;
        byte calculada = (byte)EstadoDeCorrida.Calculada;

        string sql = $"""
            SELECT (SELECT COUNT_BIG(1) FROM dbo.Empleados WHERE Activo = 1 AND (@E IS NULL OR EmpresaId = @E)),
                   (SELECT COUNT_BIG(1) FROM dbo.Periodos WHERE Estado IN ({(byte)EstadoPeriodo.Abierto}, {(byte)EstadoPeriodo.Recibido}) AND (@E IS NULL OR EmpresaId = @E)),
                   (SELECT COUNT_BIG(1) FROM dbo.Documentos AS d INNER JOIN dbo.Periodos AS p ON p.Id = d.PeriodoId
                     WHERE d.Estado = {disponible} AND p.Estado <> {cerrado} AND (@E IS NULL OR d.EmpresaId = @E)),
                   (SELECT COUNT_BIG(1) FROM dbo.CorridasDeNomina WHERE Estado = {calculada} AND (@E IS NULL OR EmpresaId = @E));

            SELECT TOP (8) p.Id, p.Estado, p.Descripcion, e.RazonSocial, p.EmpresaId,
                   (SELECT COUNT_BIG(1) FROM dbo.Documentos AS d WHERE d.PeriodoId = p.Id AND d.Estado = {disponible}
                      AND d.Tipo IN ({(byte)TipoDocumento.Incidencia}, {(byte)TipoDocumento.DatosEmpleado})),
                   (SELECT COUNT_BIG(1) FROM dbo.Documentos AS d WHERE d.PeriodoId = p.Id AND d.Estado = {disponible}
                      AND d.Tipo IN ({(byte)TipoDocumento.Resultado}, {(byte)TipoDocumento.Ajuste}))
            FROM   dbo.Periodos AS p INNER JOIN dbo.Empresas AS e ON e.Id = p.EmpresaId
            WHERE  p.Estado <> {cerrado} AND (@E IS NULL OR p.EmpresaId = @E)
            ORDER BY p.FechaApertura DESC;

            SELECT TOP (5) k.Id, k.Numero, p.Descripcion, k.EmpresaId
            FROM   dbo.CorridasDeNomina AS k INNER JOIN dbo.Periodos AS p ON p.Id = k.PeriodoId
            WHERE  k.Estado = {calculada} AND (@E IS NULL OR k.EmpresaId = @E)
            ORDER BY k.FechaCalculo DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@E", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        int empleados = 0, periodos = 0, documentos = 0, corridas = 0;

        if (await reader.ReadAsync(cancellationToken))
        {
            empleados = (int)reader.GetInt64(0);
            periodos = (int)reader.GetInt64(1);
            documentos = (int)reader.GetInt64(2);
            corridas = (int)reader.GetInt64(3);
        }

        var pendientes = new List<PendienteDto>();
        bool transversal = empresaId is null;

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid periodoId = reader.GetGuid(0);
                var estado = (EstadoPeriodo)reader.GetByte(1);
                string detalle = $"{reader.GetString(2)} · {reader.GetString(3)}";
                Guid empresa = reader.GetGuid(4);
                long delCliente = reader.GetInt64(5);
                long deResultado = reader.GetInt64(6);
                string ruta = transversal
                    ? string.Create(CultureInfo.InvariantCulture, $"periodos/{periodoId}/documentos?empresaId={empresa}")
                    : string.Create(CultureInfo.InvariantCulture, $"periodos/{periodoId}/documentos");

                if (!transversal && estado == EstadoPeriodo.Abierto && delCliente == 0)
                {
                    pendientes.Add(new PendienteDto("pendiente.subirDocumentos", detalle, ruta));
                }
                else if (!transversal && estado == EstadoPeriodo.ResultadosDisponibles && deResultado > 0)
                {
                    pendientes.Add(new PendienteDto("pendiente.descargarResultados", detalle, ruta));
                }
                else if (transversal && estado == EstadoPeriodo.Recibido)
                {
                    pendientes.Add(new PendienteDto("pendiente.procesarPeriodo", detalle, ruta));
                }
            }
        }

        if (transversal && await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                pendientes.Add(new PendienteDto(
                    "pendiente.cotejarCorrida",
                    string.Create(CultureInfo.InvariantCulture, $"#{reader.GetInt32(1)} · {reader.GetString(2)}"),
                    string.Create(CultureInfo.InvariantCulture, $"nomina/corridas/{reader.GetGuid(0)}?empresaId={reader.GetGuid(3)}")));
            }
        }

        return new ResumenDeInicioDto(empleados, periodos, documentos, corridas, pendientes);
    }
}
