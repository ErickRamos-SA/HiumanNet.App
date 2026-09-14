using Azure;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Persistence;
using HuimanNet.Infrastructure.Persistence.Semillas;
using HuimanNet.Infrastructure.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure;

/// <summary>
/// Prepara el entorno al arrancar: crea la base de datos local si falta, aplica
/// los scripts pendientes, carga los datos iniciales y crea los contenedores de
/// blobs.
/// </summary>
/// <remarks>
/// Lo invocan la API y la web en cada arranque, en cualquier entorno. Cada
/// paso tiene su interruptor en configuración:
/// <list type="table">
///   <listheader><term>Paso</term><description>Desarrollo (SQL Server local) / Azure</description></listheader>
///   <item><term><c>SqlServer:CrearBaseDeDatosSiFalta</c></term><description><c>true</c> / <c>false</c> (la crea Bicep).</description></item>
///   <item><term><c>SqlServer:AplicarScriptsAlIniciar</c></term><description><c>true</c> / <c>false</c> (los aplica CI/CD).</description></item>
///   <item><term><c>SqlServer:SembrarDatosIniciales</c></term><description><c>true</c> / <c>true</c> (sólo inserta lo que falta).</description></item>
///   <item><term><c>SqlServer:SembrarDatosDePrueba</c></term><description><c>true</c> / <c>false</c> (nunca en Azure).</description></item>
///   <item><term><c>Almacenamiento:CrearContenedoresAlIniciar</c></term><description>no aplica en local / <c>false</c> (los crea Bicep).</description></item>
/// </list>
/// <para>
/// <b>Un fallo aquí no tumba el proceso.</b> Que falte SQL Server es un
/// problema de la máquina, no del código: se registra qué falta y cómo
/// arreglarlo, y la aplicación arranca; las operaciones que necesiten ese
/// recurso fallarán con el error normal.
/// </para>
/// </remarks>
public static class PreparacionDelEntorno
{
    /// <summary>
    /// Ejecuta los pasos de preparación que la configuración tenga habilitados.
    /// </summary>
    /// <param name="servicios">Proveedor de servicios de la aplicación ya construida.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns><c>true</c> si todos los pasos habilitados terminaron bien.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="servicios"/> es <c>null</c>.
    /// </exception>
    public static async Task<bool> EjecutarAsync(
        IServiceProvider servicios, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();

        ILogger logger = ambito.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(PreparacionDelEntorno).FullName!);

        bool baseDeDatosLista = await PrepararBaseDeDatosAsync(ambito.ServiceProvider, logger, cancellationToken);
        bool todoCorrecto = baseDeDatosLista;

        if (baseDeDatosLista)
        {
            todoCorrecto &= await SembrarAsync(ambito.ServiceProvider, logger, cancellationToken);
        }

        todoCorrecto &= await PrepararAlmacenamientoAsync(ambito.ServiceProvider, logger, cancellationToken);

        return todoCorrecto;
    }

    private static async Task<bool> PrepararBaseDeDatosAsync(
        IServiceProvider servicios, ILogger logger, CancellationToken cancellationToken)
    {
        OpcionesDeSqlServer opciones =
            servicios.GetRequiredService<IOptions<OpcionesDeSqlServer>>().Value;

        if (!opciones.AplicarScriptsAlIniciar && !opciones.CrearBaseDeDatosSiFalta)
        {
            return true;
        }

        try
        {
            var inicializador = servicios.GetRequiredService<InicializadorDeBaseDeDatos>();

            if (opciones.CrearBaseDeDatosSiFalta)
            {
                await inicializador.GarantizarBaseDeDatosAsync(cancellationToken);
            }

            if (opciones.AplicarScriptsAlIniciar)
            {
                await inicializador.AplicarScriptsPendientesAsync(cancellationToken);
            }

            return true;
        }
        catch (SqlException excepcion)
        {
            logger.LogError(
                excepcion,
                "No se pudo preparar la base de datos: {Motivo}. " +
                "Compruebe que SQL Server está en marcha (servicio 'SQL Server (SQLEXPRESS)') y que " +
                "'SqlServer:CadenaDeConexion' apunta a una instancia accesible. " +
                "La aplicación arrancará, pero cualquier operación con datos fallará.",
                excepcion.Message);

            return false;
        }
        catch (InvalidOperationException excepcion)
        {
            logger.LogError(excepcion, "Configuración de base de datos incompleta: {Motivo}.", excepcion.Message);
            return false;
        }
    }

    private static async Task<bool> SembrarAsync(
        IServiceProvider servicios, ILogger logger, CancellationToken cancellationToken)
    {
        OpcionesDeSqlServer opciones =
            servicios.GetRequiredService<IOptions<OpcionesDeSqlServer>>().Value;

        if (!opciones.SembrarDatosIniciales && !opciones.SembrarDatosDePrueba)
        {
            return true;
        }

        try
        {
            if (opciones.SembrarDatosIniciales)
            {
                await servicios.GetRequiredService<SembradorInicial>().EjecutarAsync(cancellationToken);
            }

            // Después del inicial: los usuarios de prueba copian la contraseña del administrador.
            if (opciones.SembrarDatosDePrueba)
            {
                await servicios.GetRequiredService<SembradorDeDatosDePrueba>().EjecutarAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception excepcion) when (excepcion is SqlException or InvalidOperationException)
        {
            logger.LogError(
                excepcion,
                "No se pudieron cargar los datos iniciales: {Motivo}. " +
                "Revise que los scripts de base de datos estén aplicados.",
                excepcion.Message);

            return false;
        }
    }

    private static async Task<bool> PrepararAlmacenamientoAsync(
        IServiceProvider servicios, ILogger logger, CancellationToken cancellationToken)
    {
        OpcionesDeAlmacenamiento opciones =
            servicios.GetRequiredService<IOptions<OpcionesDeAlmacenamiento>>().Value;

        if (opciones.EsLocal)
        {
            ServicioDeAlmacenLocal local = servicios.GetRequiredService<ServicioDeAlmacenLocal>();
            logger.LogInformation("Almacenamiento local de documentos en {Ruta}.", local.Raiz);
            return true;
        }

        if (!opciones.CrearContenedoresAlIniciar)
        {
            return true;
        }

        try
        {
            var inicializador = servicios.GetRequiredService<InicializadorDeAlmacenamiento>();
            await inicializador.GarantizarContenedoresAsync(cancellationToken);
            return true;
        }
        catch (Exception excepcion) when (excepcion is RequestFailedException or HttpRequestException)
        {
            logger.LogError(
                excepcion,
                "No se pudo preparar el almacenamiento de documentos: {Motivo}. " +
                "Arranque el emulador Azurite, ajuste la sección 'Almacenamiento' o use el proveedor 'Local'. " +
                "La aplicación arrancará, pero la carga y la descarga de archivos fallarán.",
                excepcion.Message);

            return false;
        }
    }
}
