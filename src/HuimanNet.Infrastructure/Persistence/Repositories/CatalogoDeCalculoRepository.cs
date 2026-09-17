using System.Data;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de los catálogos del cálculo de nómina basado en ADO.NET.
/// </summary>
/// <remarks>
/// Las lecturas devuelven las entradas globales y las de la empresa indicada;
/// la resolución de vigencias y prioridades la hace la capa de aplicación.
/// Los catálogos son pequeños (decenas o cientos de filas), así que cada lectura
/// es una sola llamada sin paginación.
/// </remarks>
public sealed class CatalogoDeCalculoRepository : RepositorioSqlBase, ICatalogoDeCalculoRepository
{
    /// <summary>Columnas del parámetro de tabla con el que viajan los rangos de una tabla.</summary>
    private static readonly SqlMetaData[] ColumnasDeRango =
    [
        new("Orden", SqlDbType.Int),
        new("LimiteInferior", SqlDbType.Decimal, 18, 4),
        new("LimiteSuperior", SqlDbType.Decimal, 18, 4),
        new("CuotaFija", SqlDbType.Decimal, 18, 4),
        new("Porcentaje", SqlDbType.Decimal, 19, 8),
        new("Valor", SqlDbType.Decimal, 18, 4),
    ];

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
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ParametrosListar, cancellationToken);
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
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ParametroObtener, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearParametro(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parametro);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ParametroInsertar, cancellationToken);
        AgregarParametros(comando, parametro);
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = parametro.Clave });
        comando.Parameters.Add(Empresa(parametro.EmpresaId));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parametro);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ParametroActualizar, cancellationToken);
        AgregarParametros(comando, parametro);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarParametroAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync(Procedimientos.Catalogos.ParametroEliminar, id, cancellationToken);

    // -------------------------------------------------------------- Tablas ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TablaDeRangos>> ListarTablasAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.TablasListar, cancellationToken);
        comando.Parameters.Add(Empresa(empresaId));

        return await LeerTablasAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TablaDeRangos?> ObtenerTablaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.TablaObtener, cancellationToken);
        comando.Parameters.Add(Id(id));

        IReadOnlyList<TablaDeRangos> tablas = await LeerTablasAsync(comando, cancellationToken);
        return tablas.Count == 0 ? null : tablas[0];
    }

    /// <inheritdoc/>
    public async Task AgregarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tabla);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.TablaInsertar, cancellationToken);
        AgregarParametros(comando, tabla);
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = tabla.Clave });
        comando.Parameters.Add(Empresa(tabla.EmpresaId));
        comando.Parameters.Add(RangosComoParametro(tabla));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tabla);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.TablaActualizar, cancellationToken);
        AgregarParametros(comando, tabla);
        comando.Parameters.Add(RangosComoParametro(tabla));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarTablaAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync(Procedimientos.Catalogos.TablaEliminar, id, cancellationToken);

    // ----------------------------------------------------------- Conceptos ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConceptoDeNomina>> ListarConceptosAsync(Guid? empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ConceptosListar, cancellationToken);
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
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ConceptoObtener, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearConcepto(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(concepto);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ConceptoInsertar, cancellationToken);
        AgregarParametros(comando, concepto);
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 64) { Value = concepto.Clave });
        comando.Parameters.Add(Empresa(concepto.EmpresaId));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(concepto);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ConceptoActualizar, cancellationToken);
        AgregarParametros(comando, concepto);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarConceptoAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync(Procedimientos.Catalogos.ConceptoEliminar, id, cancellationToken);

    // ------------------------------------------------------- Explicaciones ----

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExplicacionDeCalculo>> ListarExplicacionesAsync(
        EsquemaDePago? esquema, Idioma? idioma, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ExplicacionesListar, cancellationToken);
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
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ExplicacionObtener, cancellationToken);
        comando.Parameters.Add(Id(id));

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeCatalogos.MapearExplicacion(reader) : null;
    }

    /// <inheritdoc/>
    public async Task AgregarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(explicacion);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ExplicacionInsertar, cancellationToken);
        AgregarParametros(comando, explicacion);
        comando.Parameters.Add(new SqlParameter("@Esquema", SqlDbType.TinyInt) { Value = (byte)explicacion.Esquema });
        comando.Parameters.Add(new SqlParameter("@Idioma", SqlDbType.TinyInt) { Value = (byte)explicacion.Idioma });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(explicacion);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Catalogos.ExplicacionActualizar, cancellationToken);
        AgregarParametros(comando, explicacion);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task EliminarExplicacionAsync(Guid id, CancellationToken cancellationToken = default)
        => EjecutarAsync(Procedimientos.Catalogos.ExplicacionEliminar, id, cancellationToken);

    // ------------------------------------------------------------ Auxiliar ----

    /// <summary>Agrega los valores de un parámetro de cálculo al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="p">Parámetro de cálculo.</param>
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

    /// <summary>Agrega los valores del encabezado de una tabla al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="t">Tabla de rangos.</param>
    private static void AgregarParametros(SqlCommand comando, TablaDeRangos t)
    {
        comando.Parameters.Add(Id(t.Id));
        comando.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 300) { Value = t.Descripcion });
        comando.Parameters.Add(new SqlParameter("@Desde", SqlDbType.Date) { Value = t.VigenteDesde });
        comando.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.Date) { Value = (object?)t.VigenteHasta ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = t.FechaModificacion });
    }

    /// <summary>Agrega los valores de un concepto al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="c">Concepto de nómina.</param>
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
        comando.Parameters.Add(new SqlParameter("@Alias", SqlDbType.NVarChar, 1000)
        {
            Value = c.AliasDeCotejo.Count == 0
                ? DBNull.Value
                : string.Join(LectorDeCatalogos.SeparadorDeAlias, c.AliasDeCotejo),
        });
    }

    /// <summary>Agrega los valores de una sección de la explicación al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="x">Sección de la explicación.</param>
    private static void AgregarParametros(SqlCommand comando, ExplicacionDeCalculo x)
    {
        comando.Parameters.Add(Id(x.Id));
        comando.Parameters.Add(new SqlParameter("@Orden", SqlDbType.Int) { Value = x.Orden });
        comando.Parameters.Add(new SqlParameter("@Titulo", SqlDbType.NVarChar, 200) { Value = x.Titulo });
        comando.Parameters.Add(new SqlParameter("@Cuerpo", SqlDbType.NVarChar, -1) { Value = x.Cuerpo });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.DateTimeOffset) { Value = x.FechaModificacion });
    }

    /// <summary>
    /// Crea el parámetro de tabla con los rangos, numerados desde 1 en el orden
    /// en el que los define la tabla.
    /// </summary>
    /// <param name="tabla">Tabla con sus rangos.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter RangosComoParametro(TablaDeRangos tabla)
    {
        var filas = tabla.Rangos
            .Select(static (rango, indice) => (Orden: indice + 1, Rango: rango))
            .ToList();

        return ParametrosDeTabla.Crear(
            "@Rangos", "dbo.RangosDeTablaTipo", ColumnasDeRango, filas,
            static (registro, fila) =>
            {
                registro.SetInt32(0, fila.Orden);
                registro.SetDecimal(1, fila.Rango.LimiteInferior);

                if (fila.Rango.LimiteSuperior is { } superior)
                {
                    registro.SetDecimal(2, superior);
                }
                else
                {
                    registro.SetDBNull(2);
                }

                registro.SetDecimal(3, fila.Rango.CuotaFija);
                registro.SetDecimal(4, fila.Rango.Porcentaje);
                registro.SetDecimal(5, fila.Rango.Valor);
            });
    }

    /// <summary>
    /// Lee las tablas de un procedimiento con dos resultados: primero los
    /// encabezados y después los rangos.
    /// </summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las tablas que tienen al menos un rango.</returns>
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

    /// <summary>Ejecuta un procedimiento que sólo recibe el identificador.</summary>
    /// <param name="procedimiento">Procedimiento con el parámetro <c>@Id</c>.</param>
    /// <param name="id">Identificador del elemento.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al ejecutarlo.</returns>
    private async Task EjecutarAsync(string procedimiento, Guid id, CancellationToken cancellationToken)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(procedimiento, cancellationToken);
        comando.Parameters.Add(Id(id));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Crea el parámetro <c>@Id</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Id(Guid id) => new("@Id", SqlDbType.UniqueIdentifier) { Value = id };

    /// <summary>Crea el parámetro <c>@EmpresaId</c>.</summary>
    /// <param name="empresaId">Empresa, o <c>null</c> para el catálogo general.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Empresa(Guid? empresaId)
        => new("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value };
}
