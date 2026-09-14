using System.Data;
using System.Reflection;
using HuimanNet.Infrastructure.Persistence.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Infrastructure.Persistence;

/// <summary>
/// Aplica los scripts SQL versionados que aún no se han ejecutado.
/// </summary>
/// <remarks>
/// Sustituye a las migraciones de un ORM (ESPECIFICACION.md §6.1). Los scripts
/// viven como recursos incrustados en <c>Persistence/Scripts</c>, se ordenan por
/// nombre y cada uno se aplica <b>una sola vez</b>, anotado en
/// <c>dbo.__HistorialScripts</c>.
/// <para>
/// En producción conviene ejecutarlos desde el flujo de CI/CD y dejar
/// <c>AplicarScriptsAlIniciar</c> en <c>false</c>: así el arranque de la
/// aplicación no necesita permisos de DDL.
/// </para>
/// </remarks>
public sealed class InicializadorDeBaseDeDatos
{
    private const string PrefijoDeRecursos = "HuimanNet.Infrastructure.Persistence.Scripts.";

    private readonly ISqlConnectionFactory _factory;
    private readonly ILogger<InicializadorDeBaseDeDatos> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="InicializadorDeBaseDeDatos"/>.
    /// </summary>
    /// <param name="factory">Fábrica de conexiones.</param>
    /// <param name="logger">Registro de eventos.</param>
    public InicializadorDeBaseDeDatos(
        ISqlConnectionFactory factory, ILogger<InicializadorDeBaseDeDatos> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Aplica todos los scripts pendientes, en orden de nombre.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El número de scripts aplicados en esta ejecución.</returns>
    /// <exception cref="SqlException">
    /// Se propaga si algún lote falla; el script queda sin anotar y volverá a
    /// intentarse en el siguiente arranque.
    /// </exception>
    public async Task<int> AplicarScriptsPendientesAsync(CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = _factory.Crear();
        await conexion.OpenAsync(cancellationToken);

        await GarantizarTablaDeHistorialAsync(conexion, cancellationToken);

        HashSet<string> aplicados = await ObtenerAplicadosAsync(conexion, cancellationToken);
        int ejecutados = 0;

        foreach (string nombre in ObtenerNombresDeScripts())
        {
            if (aplicados.Contains(nombre))
            {
                continue;
            }

            _logger.LogInformation("Aplicando script de base de datos {Script}.", nombre);

            foreach (string lote in DividirEnLotes(LeerScript(nombre)))
            {
                await using SqlCommand comando = conexion.CreateCommand();
                comando.CommandText = lote;
                comando.CommandTimeout = _factory.TiempoDeEsperaComandoSegundos;
                await comando.ExecuteNonQueryAsync(cancellationToken);
            }

            await AnotarAplicadoAsync(conexion, nombre, cancellationToken);
            ejecutados++;
        }

        if (ejecutados == 0)
        {
            _logger.LogInformation("La base de datos ya estaba al día: no había scripts pendientes.");
        }

        return ejecutados;
    }

    /// <summary>
    /// Crea la base de datos indicada en la cadena de conexión si todavía no existe.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns><c>true</c> si la base de datos se creó en esta ejecución.</returns>
    /// <remarks>
    /// Pensado para desarrollo con SQL Server local (Express o Developer): se
    /// conecta a <c>master</c> con las mismas credenciales. En Azure SQL la base
    /// de datos la aprovisiona la plantilla de infraestructura y este paso no se
    /// ejecuta (<c>SqlServer:CrearBaseDeDatosSiFalta = false</c>).
    /// </remarks>
    /// <exception cref="SqlException">Se propaga si no hay conexión o faltan permisos.</exception>
    public async Task<bool> GarantizarBaseDeDatosAsync(CancellationToken cancellationToken = default)
    {
        string cadenaOriginal;

        await using (SqlConnection original = _factory.Crear())
        {
            cadenaOriginal = original.ConnectionString;
        }

        var constructor = new SqlConnectionStringBuilder(cadenaOriginal);
        string nombre = constructor.InitialCatalog;

        if (string.IsNullOrWhiteSpace(nombre) || string.Equals(nombre, "master", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        constructor.InitialCatalog = "master";

        await using var conexion = new SqlConnection(constructor.ConnectionString);
        await conexion.OpenAsync(cancellationToken);

        await using SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = """
            IF DB_ID(@Nombre) IS NULL
            BEGIN
                DECLARE @sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@Nombre) + N';';
                EXEC (@sql);
                SELECT CAST(1 AS BIT);
            END
            ELSE
                SELECT CAST(0 AS BIT);
            """;
        comando.CommandTimeout = Math.Max(_factory.TiempoDeEsperaComandoSegundos, 120);
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 128) { Value = nombre });

        bool creada = await comando.ExecuteScalarAsync(cancellationToken) is true;

        if (creada)
        {
            _logger.LogInformation("Base de datos {BaseDeDatos} creada en la instancia local.", nombre);

            // Las conexiones fallidas previas quedan en el pool con error; se descartan.
            SqlConnection.ClearAllPools();
        }

        return creada;
    }

    private static IEnumerable<string> ObtenerNombresDeScripts()
        => typeof(InicializadorDeBaseDeDatos).Assembly
            .GetManifestResourceNames()
            .Where(static nombre =>
                nombre.StartsWith(PrefijoDeRecursos, StringComparison.Ordinal)
                && nombre.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static nombre => nombre[PrefijoDeRecursos.Length..])
            .OrderBy(static nombre => nombre, StringComparer.Ordinal);

    private static string LeerScript(string nombre)
    {
        Assembly ensamblado = typeof(InicializadorDeBaseDeDatos).Assembly;

        using Stream flujo = ensamblado.GetManifestResourceStream(PrefijoDeRecursos + nombre)
            ?? throw new InvalidOperationException($"No se encontró el script incrustado '{nombre}'.");

        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }

    /// <summary>
    /// Separa el script en lotes por el delimitador <c>GO</c>.
    /// </summary>
    /// <param name="script">Contenido completo del script.</param>
    /// <returns>Los lotes no vacíos, en orden.</returns>
    /// <remarks>
    /// <c>GO</c> no es una instrucción de T-SQL sino un separador que interpreta
    /// la herramienta cliente: hay que dividir aquí porque ADO.NET no lo entiende.
    /// </remarks>
    private static IEnumerable<string> DividirEnLotes(string script)
        => script
            .Split(["\r\nGO", "\nGO"], StringSplitOptions.None)
            .Select(static lote => lote.Trim())
            .Where(static lote => lote.Length > 0);

    private async Task GarantizarTablaDeHistorialAsync(
        SqlConnection conexion, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.__HistorialScripts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.__HistorialScripts
                (
                    Nombre        NVARCHAR(200)     NOT NULL,
                    FechaAplicado DATETIMEOFFSET(7) NOT NULL,
                    CONSTRAINT PK___HistorialScripts PRIMARY KEY CLUSTERED (Nombre)
                );
            END;
            """;

        await using SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = _factory.TiempoDeEsperaComandoSegundos;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<HashSet<string>> ObtenerAplicadosAsync(
        SqlConnection conexion, CancellationToken cancellationToken)
    {
        await using SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = "SELECT Nombre FROM dbo.__HistorialScripts;";
        comando.CommandTimeout = _factory.TiempoDeEsperaComandoSegundos;

        var aplicados = new HashSet<string>(StringComparer.Ordinal);

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            aplicados.Add(reader.GetString(0));
        }

        return aplicados;
    }

    private async Task AnotarAplicadoAsync(
        SqlConnection conexion, string nombre, CancellationToken cancellationToken)
    {
        await using SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO dbo.__HistorialScripts (Nombre, FechaAplicado)
            VALUES (@Nombre, SYSDATETIMEOFFSET());
            """;
        comando.CommandTimeout = _factory.TiempoDeEsperaComandoSegundos;
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 200) { Value = nombre });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
