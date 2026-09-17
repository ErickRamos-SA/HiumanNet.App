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
    /// <summary>Prefijo de los scripts SQL versionados incrustados en el ensamblado.</summary>
    private const string PrefijoDeRecursos = "HuimanNet.Infrastructure.Persistence.Scripts.";

    /// <summary>Prefijo de los procedimientos almacenados incrustados en el ensamblado.</summary>
    private const string PrefijoDeProcedimientos = "HuimanNet.Infrastructure.Persistence.Procedimientos.";

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

        foreach (string nombre in ObtenerNombresDeRecursos(PrefijoDeRecursos))
        {
            if (aplicados.Contains(nombre))
            {
                continue;
            }

            _logger.LogInformation("Aplicando script de base de datos {Script}.", nombre);

            foreach (string lote in DividirEnLotes(LeerRecurso(PrefijoDeRecursos, nombre)))
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

        await AplicarProcedimientosAsync(conexion, cancellationToken);

        return ejecutados;
    }

    /// <summary>
    /// Vuelve a crear todos los procedimientos almacenados incrustados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El número de archivos de procedimientos aplicados.</returns>
    /// <remarks>
    /// A diferencia de los scripts numerados, los procedimientos no se anotan en
    /// el historial: son <c>CREATE OR ALTER</c> y se aplican enteros cada vez,
    /// de modo que la base de datos siempre tiene la versión que espera el
    /// código que la está usando. Aplicarlos exige permisos de DDL, igual que
    /// los scripts.
    /// </remarks>
    /// <exception cref="SqlException">Se propaga si algún lote falla.</exception>
    public async Task<int> AplicarProcedimientosAsync(CancellationToken cancellationToken = default)
    {
        await using SqlConnection conexion = _factory.Crear();
        await conexion.OpenAsync(cancellationToken);

        return await AplicarProcedimientosAsync(conexion, cancellationToken);
    }

    /// <summary>Aplica los procedimientos sobre una conexión ya abierta.</summary>
    /// <param name="conexion">Conexión abierta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El número de archivos de procedimientos aplicados.</returns>
    private async Task<int> AplicarProcedimientosAsync(
        SqlConnection conexion, CancellationToken cancellationToken)
    {
        int aplicados = 0;

        foreach (string nombre in ObtenerNombresDeRecursos(PrefijoDeProcedimientos))
        {
            foreach (string lote in DividirEnLotes(LeerRecurso(PrefijoDeProcedimientos, nombre)))
            {
                await using SqlCommand comando = conexion.CreateCommand();
                comando.CommandText = lote;
                comando.CommandTimeout = _factory.TiempoDeEsperaComandoSegundos;
                await comando.ExecuteNonQueryAsync(cancellationToken);
            }

            aplicados++;
        }

        _logger.LogInformation("Procedimientos almacenados aplicados: {Archivos} archivos.", aplicados);

        return aplicados;
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

    /// <summary>Lista los archivos SQL incrustados bajo un prefijo.</summary>
    /// <param name="prefijo">Prefijo del recurso: scripts o procedimientos.</param>
    /// <returns>Los nombres sin prefijo, en orden ordinal, que es el orden de aplicación.</returns>
    private static IEnumerable<string> ObtenerNombresDeRecursos(string prefijo)
        => typeof(InicializadorDeBaseDeDatos).Assembly
            .GetManifestResourceNames()
            .Where(nombre =>
                nombre.StartsWith(prefijo, StringComparison.Ordinal)
                && nombre.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(nombre => nombre[prefijo.Length..])
            .OrderBy(static nombre => nombre, StringComparer.Ordinal);

    /// <summary>Lee el contenido de un archivo SQL incrustado.</summary>
    /// <param name="prefijo">Prefijo del recurso: scripts o procedimientos.</param>
    /// <param name="nombre">Nombre del archivo, sin prefijo.</param>
    /// <returns>El texto del archivo.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el recurso no existe.</exception>
    private static string LeerRecurso(string prefijo, string nombre)
    {
        Assembly ensamblado = typeof(InicializadorDeBaseDeDatos).Assembly;

        using Stream flujo = ensamblado.GetManifestResourceStream(prefijo + nombre)
            ?? throw new InvalidOperationException($"No se encontró el recurso incrustado '{nombre}'.");

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

    /// <summary>Crea la tabla que anota los scripts aplicados, si no existe.</summary>
    /// <param name="conexion">Conexión abierta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza cuando la tabla existe.</returns>
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

    /// <summary>Lee los scripts ya aplicados.</summary>
    /// <param name="conexion">Conexión abierta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los nombres de los scripts aplicados.</returns>
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

    /// <summary>Anota un script como aplicado.</summary>
    /// <param name="conexion">Conexión abierta.</param>
    /// <param name="nombre">Nombre del script.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al anotarlo.</returns>
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
