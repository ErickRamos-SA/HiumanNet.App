using System.Data;
using System.Globalization;
using System.Text;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de los catálogos del cálculo de nómina basado en ADO.NET.
/// </summary>
/// <remarks>
/// Las lecturas devuelven las entradas globales y las de la empresa indicada;
/// la resolución de vigencias y prioridades la hace la capa de aplicación.
/// Los catálogos son pequeños (decenas o cientos de filas), así que cada lectura
/// es una sola sentencia sin paginación.
/// </remarks>
public sealed class CatalogoDeCalculoRepository : RepositorioSqlBase, ICatalogoDeCalculoRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CatalogoDeCalculoRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public CatalogoDeCalculoRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    // ---------------------------------------------------------- Parámetros ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ParametroDeCalculo>> ListarParametrosAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCatalogos.ColumnasDeParametro}
            FROM   dbo.ParametrosDeCalculo AS p
            WHERE  p.EmpresaId IS NULL OR p.EmpresaId = @EmpresaId
            ORDER BY p.Grupo, p.Clave, p.VigenteDesde;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Empresa(empresaId));

        var lista = new List<ParametroDeCalculo>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeCatalogos.MapearParametro(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<ParametroDeCalculo?> ObtenerParametroAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {LectorDeCatalogos.ColumnasDeParametro} FROM dbo.ParametrosDeCalculo AS p WHERE p.Id = @Id;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearParametro(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parametro);

        const string sql = """
            INSERT INTO dbo.ParametrosDeCalculo
                (Id, Clave, Descripcion, Grupo, Valor, Unidad, EmpresaId, VigenteDesde, VigenteHasta, FechaModificacion)
            VALUES (@Id, @Clave, @Descripcion, @Grupo, @Valor, @Unidad, @EmpresaId, @Desde, @Hasta, @Fecha);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, parametro);
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = parametro.Clave });
        comando.Parameters.Add(Empresa(parametro.EmpresaId));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parametro);

        const string sql = """
            UPDATE dbo.ParametrosDeCalculo
            SET    Descripcion = @Descripcion, Grupo = @Grupo, Valor = @Valor, Unidad = @Unidad,
                   VigenteDesde = @Desde, VigenteHasta = @Hasta, FechaModificacion = @Fecha
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, parametro);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarParametroAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync("DELETE FROM dbo.ParametrosDeCalculo WHERE Id = @Id;", id, cancellationToken);

    // -------------------------------------------------------------- Tablas ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TablaDeRangos>> ListarTablasAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCatalogos.ColumnasDeTabla}
            FROM   dbo.TablasDeRangos AS t
            WHERE  t.EmpresaId IS NULL OR t.EmpresaId = @EmpresaId
            ORDER BY t.Clave, t.VigenteDesde;

            SELECT {LectorDeCatalogos.ColumnasDeRango}
            FROM   dbo.RangosDeTabla AS r
            INNER JOIN dbo.TablasDeRangos AS t ON t.Id = r.TablaId
            WHERE  t.EmpresaId IS NULL OR t.EmpresaId = @EmpresaId
            ORDER BY r.TablaId, r.Orden;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Empresa(empresaId));

        return await LeerTablasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TablaDeRangos?> ObtenerTablaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCatalogos.ColumnasDeTabla} FROM dbo.TablasDeRangos AS t WHERE t.Id = @Id;
            SELECT {LectorDeCatalogos.ColumnasDeRango} FROM dbo.RangosDeTabla AS r WHERE r.TablaId = @Id ORDER BY r.Orden;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Id(id));

        IReadOnlyList<TablaDeRangos> tablas = await LeerTablasAsync(comando, cancellationToken);
        return tablas.Count == 0 ? null : tablas[0];
    }

    /// <inheritdoc/>
    public async Task AgregarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tabla);

        const string sql = """
            INSERT INTO dbo.TablasDeRangos (Id, Clave, Descripcion, EmpresaId, VigenteDesde, VigenteHasta, FechaModificacion)
            VALUES (@Id, @Clave, @Descripcion, @EmpresaId, @Desde, @Hasta, @Fecha);
            """;

        await using (SqlCommand comando = await CrearComandoAsync(sql, cancellationToken))
        {
            AgregarParametros(comando, tabla);
            comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = tabla.Clave });
            comando.Parameters.Add(Empresa(tabla.EmpresaId));
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await GuardarRangosAsync(tabla, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tabla);

        const string sql = """
            UPDATE dbo.TablasDeRangos
            SET    Descripcion = @Descripcion, VigenteDesde = @Desde, VigenteHasta = @Hasta, FechaModificacion = @Fecha
            WHERE  Id = @Id;
            """;

        await using (SqlCommand comando = await CrearComandoAsync(sql, cancellationToken))
        {
            AgregarParametros(comando, tabla);
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await GuardarRangosAsync(tabla, cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarTablaAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync("DELETE FROM dbo.TablasDeRangos WHERE Id = @Id;", id, cancellationToken);

    // ----------------------------------------------------------- Conceptos ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConceptoDeNomina>> ListarConceptosAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCatalogos.ColumnasDeConcepto}
            FROM   dbo.ConceptosDeNomina AS c
            WHERE  c.EmpresaId IS NULL OR c.EmpresaId = @EmpresaId
            ORDER BY c.Orden, c.Clave;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Empresa(empresaId));

        var lista = new List<ConceptoDeNomina>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeCatalogos.MapearConcepto(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<ConceptoDeNomina?> ObtenerConceptoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {LectorDeCatalogos.ColumnasDeConcepto} FROM dbo.ConceptosDeNomina AS c WHERE c.Id = @Id;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearConcepto(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(concepto);

        const string sql = """
            INSERT INTO dbo.ConceptosDeNomina
                (Id, Clave, Nombre, Descripcion, Tipo, Esquemas, Orden, Formula, VisibleEnRecibo, Activo, EmpresaId, FechaModificacion)
            VALUES (@Id, @Clave, @Nombre, @Descripcion, @Tipo, @Esquemas, @Orden, @Formula, @Visible, @Activo, @EmpresaId, @Fecha);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, concepto);
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = concepto.Clave });
        comando.Parameters.Add(Empresa(concepto.EmpresaId));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(concepto);

        const string sql = """
            UPDATE dbo.ConceptosDeNomina
            SET    Nombre = @Nombre, Descripcion = @Descripcion, Tipo = @Tipo, Esquemas = @Esquemas, Orden = @Orden,
                   Formula = @Formula, VisibleEnRecibo = @Visible, Activo = @Activo, FechaModificacion = @Fecha
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, concepto);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarConceptoAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync("DELETE FROM dbo.ConceptosDeNomina WHERE Id = @Id;", id, cancellationToken);

    // ------------------------------------------------------- Explicaciones ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExplicacionDeCalculo>> ListarExplicacionesAsync(
        EsquemaDePago? esquema, Idioma? idioma, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeCatalogos.ColumnasDeExplicacion}
            FROM   dbo.ExplicacionesDeCalculo AS x
            WHERE  (@Esquema IS NULL OR x.Esquema = @Esquema) AND (@Idioma IS NULL OR x.Idioma = @Idioma)
            ORDER BY x.Esquema, x.Idioma, x.Orden;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Esquema", SqlDbType.TinyInt) { Value = esquema is null ? DBNull.Value : (byte)esquema.Value });
        comando.Parameters.Add(new SqlParameter("@Idioma", SqlDbType.TinyInt) { Value = idioma is null ? DBNull.Value : (byte)idioma.Value });

        var lista = new List<ExplicacionDeCalculo>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeCatalogos.MapearExplicacion(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task<ExplicacionDeCalculo?> ObtenerExplicacionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {LectorDeCatalogos.ColumnasDeExplicacion} FROM dbo.ExplicacionesDeCalculo AS x WHERE x.Id = @Id;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearExplicacion(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(explicacion);

        const string sql = """
            INSERT INTO dbo.ExplicacionesDeCalculo (Id, Esquema, Idioma, Orden, Titulo, Cuerpo, FechaModificacion)
            VALUES (@Id, @Esquema, @Idioma, @Orden, @Titulo, @Cuerpo, @Fecha);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, explicacion);
        comando.Parameters.Add(new SqlParameter("@Esquema", SqlDbType.TinyInt) { Value = (byte)explicacion.Esquema });
        comando.Parameters.Add(new SqlParameter("@Idioma", SqlDbType.TinyInt) { Value = (byte)explicacion.Idioma });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(explicacion);

        const string sql = """
            UPDATE dbo.ExplicacionesDeCalculo
            SET    Orden = @Orden, Titulo = @Titulo, Cuerpo = @Cuerpo, FechaModificacion = @Fecha
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, explicacion);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarExplicacionAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync("DELETE FROM dbo.ExplicacionesDeCalculo WHERE Id = @Id;", id, cancellationToken);

    // ------------------------------------------------------------ Auxiliar ----

    private static void AgregarParametros(SqlCommand comando, ParametroDeCalculo p)
    {
        comando.Parameters.Add(Id(p.Id));
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 300) { Value = p.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Grupo", SqlDbType.NVarChar, 50) { Value = p.Grupo });
        comando.Parameters.Add(new SqlParameter("@Valor", SqlDbType.Decimal) { Precision = 19, Scale = 8, Value = p.Valor });
        comando.Parameters.Add(new SqlParameter("@Unidad", SqlDbType.NVarChar, 20) { Value = p.Unidad });
        comando.Parameters.Add(new SqlParameter("@Desde", SqlDbType.Date) { Value = p.VigenteDesde });
        comando.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.Date) { Value = (object?)p.VigenteHasta ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = p.FechaModificacion });
    }

    private static void AgregarParametros(SqlCommand comando, TablaDeRangos t)
    {
        comando.Parameters.Add(Id(t.Id));
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 300) { Value = t.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Desde", SqlDbType.Date) { Value = t.VigenteDesde });
        comando.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.Date) { Value = (object?)t.VigenteHasta ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = t.FechaModificacion });
    }

    private static void AgregarParametros(SqlCommand comando, ConceptoDeNomina c)
    {
        comando.Parameters.Add(Id(c.Id));
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 120) { Value = c.Nombre });
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 1000) { Value = c.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.TinyInt) { Value = (byte)c.Tipo });
        comando.Parameters.Add(new SqlParameter("@Esquemas", SqlDbType.TinyInt) { Value = (byte)c.Esquemas });
        comando.Parameters.Add(new SqlParameter("@Orden", SqlDbType.Int) { Value = c.Orden });
        comando.Parameters.Add(new SqlParameter("@Formula", SqlDbType.NVarChar, ConceptoDeNomina.LongitudMaximaFormula) { Value = c.Formula });
        comando.Parameters.Add(new SqlParameter("@Visible", SqlDbType.Bit) { Value = c.VisibleEnRecibo });
        comando.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = c.Activo });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = c.FechaModificacion });
    }

    private static void AgregarParametros(SqlCommand comando, ExplicacionDeCalculo x)
    {
        comando.Parameters.Add(Id(x.Id));
        comando.Parameters.Add(new SqlParameter("@Orden", SqlDbType.Int) { Value = x.Orden });
        comando.Parameters.Add(new SqlParameter("@Titulo", SqlDbType.NVarChar, 200) { Value = x.Titulo });
        comando.Parameters.Add(new SqlParameter("@Cuerpo", SqlDbType.NVarChar, -1) { Value = x.Cuerpo });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = x.FechaModificacion });
    }

    /// <summary>
    /// Sustituye los rangos de una tabla en un solo comando.
    /// </summary>
    private async Task GuardarRangosAsync(TablaDeRangos tabla, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("DELETE FROM dbo.RangosDeTabla WHERE TablaId = @Id;");
        sql.Append(" INSERT INTO dbo.RangosDeTabla (TablaId, Orden, LimiteInferior, LimiteSuperior, CuotaFija, Porcentaje, Valor) VALUES ");

        for (int i = 0; i < tabla.Rangos.Count; i++)
        {
            sql.Append(i == 0 ? string.Empty : ", ")
               .Append(CultureInfo.InvariantCulture, $"(@Id, {i + 1}, @Li{i}, @Ls{i}, @Cf{i}, @Pc{i}, @Va{i})");
        }

        sql.Append(';');

        await using SqlCommand comando = await CrearComandoAsync(sql.ToString(), cancellationToken);
        comando.Parameters.Add(Id(tabla.Id));

        for (int i = 0; i < tabla.Rangos.Count; i++)
        {
            RangoDeTabla r = tabla.Rangos[i];
            comando.Parameters.Add(Importe($"@Li{i}", r.LimiteInferior));
            comando.Parameters.Add(Importe($"@Ls{i}", r.LimiteSuperior));
            comando.Parameters.Add(Importe($"@Cf{i}", r.CuotaFija));
            comando.Parameters.Add(new SqlParameter(Nombre($"@Pc{i}"), SqlDbType.Decimal) { Precision = 19, Scale = 8, Value = r.Porcentaje });
            comando.Parameters.Add(Importe($"@Va{i}", r.Valor));
        }

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<TablaDeRangos>> LeerTablasAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        var encabezados = new List<(Guid Id, string Clave, string Descripcion, Guid? EmpresaId, DateOnly Desde, DateOnly? Hasta, DateTimeOffset Fecha)>();
        var rangos = new Dictionary<Guid, List<RangoDeTabla>>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            encabezados.Add((
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetFieldValue<DateOnly>(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
                reader.GetDateTimeOffset(6)));
        }

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                (Guid tablaId, RangoDeTabla rango) = LectorDeCatalogos.MapearRango(reader);

                if (!rangos.TryGetValue(tablaId, out List<RangoDeTabla>? lista))
                {
                    lista = [];
                    rangos[tablaId] = lista;
                }

                lista.Add(rango);
            }
        }

        var tablas = new List<TablaDeRangos>(encabezados.Count);

        foreach (var e in encabezados)
        {
            if (rangos.TryGetValue(e.Id, out List<RangoDeTabla>? lista) && lista.Count > 0)
            {
                tablas.Add(TablaDeRangos.Rehidratar(e.Id, e.Clave, e.Descripcion, e.EmpresaId, e.Desde, e.Hasta, e.Fecha, lista));
            }
        }

        return tablas;
    }

    private async Task EjecutarAsync(string sql, Guid id, CancellationToken cancellationToken)
    {
        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(Id(id));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlParameter Id(Guid id) => new("@Id", SqlDbType.UniqueIdentifier) { Value = id };

    private static SqlParameter Empresa(Guid? empresaId)
        => new("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value };

    private static SqlParameter Importe(string nombre, decimal? valor)
        => new(Nombre(nombre), SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)valor ?? DBNull.Value };

    private static string Nombre(string nombre) => nombre;
}
