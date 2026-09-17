using HuimanNet.Application;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Piezas comunes de las pruebas que corren contra un SQL Server real: base de
/// datos exclusiva que se recrea en cada ejecución y contenedor con las capas
/// de aplicación e infraestructura, igual que el arranque de la aplicación.
/// </summary>
/// <remarks>
/// La cadena de conexión se toma de la variable de entorno
/// <c>HUIMANNET_SQL_PRUEBAS</c> o, si no existe, de SQL Server Express local.
/// Por seguridad, sólo se borra una base de datos cuyo nombre termine en
/// <c>_Pruebas</c>.
/// </remarks>
internal static class EntornoSqlDePrueba
{
    private const string CadenaPredeterminada =
        "Server=localhost\\SQLEXPRESS;Database=HuimanNet_Pruebas;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;Connect Timeout=5";

    /// <summary>Obtiene el token de cancelación de las pruebas.</summary>
    public static CancellationToken Ct => CancellationToken.None;

    /// <summary>Devuelve la cadena de conexión de pruebas.</summary>
    /// <param name="baseDeDatos">Base de datos exclusiva de la prueba, o <c>null</c> para la predeterminada.</param>
    public static string Cadena(string? baseDeDatos = null)
    {
        string cadena = Environment.GetEnvironmentVariable("HUIMANNET_SQL_PRUEBAS") ?? CadenaPredeterminada;

        return baseDeDatos is null
            ? cadena
            : new SqlConnectionStringBuilder(cadena) { InitialCatalog = baseDeDatos }.ConnectionString;
    }

    /// <summary>Borra la base de datos de pruebas; devuelve el motivo si el servidor no responde.</summary>
    public static async Task<string?> RecrearBaseDeDatosAsync(string cadena)
    {
        var constructor = new SqlConnectionStringBuilder(cadena);
        string nombre = constructor.InitialCatalog;

        if (!nombre.EndsWith("_Pruebas", StringComparison.OrdinalIgnoreCase))
        {
            return $"por seguridad, la base de datos de pruebas debe terminar en '_Pruebas' (se recibió '{nombre}')";
        }

        constructor.InitialCatalog = "master";

        try
        {
            await using var conexion = new SqlConnection(constructor.ConnectionString);
            await conexion.OpenAsync(Ct);

            await using SqlCommand comando = conexion.CreateCommand();
            comando.CommandText = """
                IF DB_ID(@Nombre) IS NOT NULL
                BEGIN
                    DECLARE @sql NVARCHAR(600) =
                        N'ALTER DATABASE ' + QUOTENAME(@Nombre) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
                        N'DROP DATABASE ' + QUOTENAME(@Nombre) + N';';
                    EXEC (@sql);
                END
                """;
            comando.Parameters.AddWithValue("@Nombre", nombre);
            comando.CommandTimeout = 120;
            await comando.ExecuteNonQueryAsync(Ct);
        }
        catch (SqlException excepcion)
        {
            return excepcion.Message;
        }

        SqlConnection.ClearAllPools();
        return null;
    }

    /// <summary>Construye el contenedor de servicios con la configuración de pruebas.</summary>
    /// <param name="cadena">Cadena de conexión de la base de datos de pruebas.</param>
    /// <param name="correoAdministrador">Correo del administrador inicial que siembra el arranque.</param>
    /// <param name="adicionales">Valores de configuración que se agregan o sobrescriben.</param>
    public static ServiceProvider ConstruirServicios(
        string cadena, string correoAdministrador, IReadOnlyDictionary<string, string?>? adicionales = null)
    {
        var valores = new Dictionary<string, string?>
        {
            ["SqlServer:CadenaDeConexion"] = cadena,
            ["SqlServer:AplicarScriptsAlIniciar"] = "true",
            ["SqlServer:CrearBaseDeDatosSiFalta"] = "true",
            ["SqlServer:SembrarDatosIniciales"] = "true",
            ["Identidad:Modo"] = "Local",
            ["Identidad:AdministradorInicial:Correo"] = correoAdministrador,
            ["Identidad:AdministradorInicial:NombreCompleto"] = "Administrador de pruebas",
            ["Almacenamiento:Proveedor"] = "Local",
            ["Almacenamiento:RutaLocal"] = Path.Combine(Path.GetTempPath(), "huimannet-pruebas"),
            ["Notificaciones:Habilitado"] = "false",
        };

        foreach ((string clave, string? valor) in adicionales ?? new Dictionary<string, string?>())
        {
            valores[clave] = valor;
        }

        IConfiguration configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AgregarCapaDeAplicacion();
        services.AgregarCapaDeInfraestructura(configuracion);
        services.AddSingleton<UsuarioDePrueba>();
        services.AddSingleton<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioDePrueba>());

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <summary>Lee un usuario por correo en un ámbito nuevo.</summary>
    public static async Task<Usuario> ObtenerUsuarioAsync(IServiceProvider servicios, string correo)
    {
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        IUsuarioRepository usuarios = ambito.ServiceProvider.GetRequiredService<IUsuarioRepository>();

        return await usuarios.ObtenerPorCorreoAsync(correo, Ct)
            ?? throw new InvalidOperationException($"No existe el usuario '{correo}'.");
    }
}
