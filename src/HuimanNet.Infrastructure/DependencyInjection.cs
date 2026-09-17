using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using HuimanNet.Infrastructure.Archivos;
using HuimanNet.Infrastructure.Configuracion;
using HuimanNet.Infrastructure.Identity;
using HuimanNet.Infrastructure.Notifications;
using HuimanNet.Infrastructure.Persistence;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Queries;
using HuimanNet.Infrastructure.Persistence.Repositories;
using HuimanNet.Infrastructure.Persistence.Semillas;
using HuimanNet.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure;

/// <summary>
/// Registro de la capa de infraestructura en el contenedor de dependencias.
/// </summary>
/// <remarks>
/// La consumen los tres anfitriones: la API (Native AOT), la web Blazor Server y
/// las pruebas de integración. El registro es explícito, sin escaneo de
/// ensamblados, para sobrevivir al recorte.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia ADO.NET, identidad local, almacenamiento de
    /// documentos, lectura de archivos tabulares y avisos.
    /// </summary>
    /// <param name="services">Colección de servicios del anfitrión.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="services"/> o <paramref name="configuration"/> son <c>null</c>.
    /// </exception>
    public static IServiceCollection AgregarCapaDeInfraestructura(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OpcionesDeSqlServer>(configuration.GetSection(OpcionesDeSqlServer.Seccion));
        services.Configure<OpcionesDeAlmacenamiento>(configuration.GetSection(OpcionesDeAlmacenamiento.Seccion));
        services.Configure<OpcionesDeNotificaciones>(configuration.GetSection(OpcionesDeNotificaciones.Seccion));
        services.Configure<OpcionesDeCarga>(configuration.GetSection(OpcionesDeCarga.Seccion));
        services.Configure<OpcionesDeIdentidad>(configuration.GetSection(OpcionesDeIdentidad.Seccion));

        services.TryAddSingleton(TimeProvider.System);

        services.AgregarPersistencia();
        services.AgregarIdentidad();
        services.AgregarAlmacenamiento(configuration);
        services.AgregarNotificaciones();
        services.AgregarPoliticaDeCarga();

        services.AddSingleton<ILectorDeArchivosTabulares, LectorDeArchivosTabulares>();

        return services;
    }

    /// <summary>
    /// Registra la sesión SQL por petición, los repositorios, las consultas, el
    /// inicializador de la base de datos y los sembradores.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void AgregarPersistencia(this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        // Una conexión (y a lo sumo una transacción) por petición: es la pieza
        // que sustituye al DbContext de un ORM.
        services.AddScoped<SesionSql>();
        services.AddScoped<ISesionSql>(sp => sp.GetRequiredService<SesionSql>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SesionSql>());

        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IRazonSocialRepository, RazonSocialRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPeriodoRepository, PeriodoRepository>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<ICatalogoDeCalculoRepository, CatalogoDeCalculoRepository>();
        services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
        services.AddScoped<ICorridaDeNominaRepository, CorridaDeNominaRepository>();
        services.AddScoped<ICotejoRepository, CotejoRepository>();

        services.AddScoped<IConsultasDocumentos, ConsultasDocumentos>();
        services.AddScoped<IConsultasPeriodos, ConsultasPeriodos>();
        services.AddScoped<IConsultasEmpresas, ConsultasEmpresas>();
        services.AddScoped<IConsultasAuditoria, ConsultasAuditoria>();
        services.AddScoped<IConsultasEmpleados, ConsultasEmpleados>();
        services.AddScoped<IConsultasIncidencias, ConsultasIncidencias>();
        services.AddScoped<IConsultasNomina, ConsultasNomina>();
        services.AddScoped<IConsultasUsuarios, ConsultasUsuarios>();
        services.AddScoped<IConsultasInicio, ConsultasInicio>();

        services.AddSingleton<InicializadorDeBaseDeDatos>();
        services.AddScoped<SembradorInicial>();
        services.AddScoped<SembradorDeDatosDePrueba>();
    }

    /// <summary>
    /// Registra la resolución de usuarios por claims, el hash de contraseñas y
    /// la emisión de tokens del modo local.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void AgregarIdentidad(this IServiceCollection services)
    {
        services.AddSingleton<LectorDeIdentidadPorClaims>();
        services.AddSingleton<IHasherDeContrasenas, HasherDeContrasenasPbkdf2>();
        services.AddSingleton<MaterialDeFirmaLocal>();
        services.AddSingleton<IEmisorDeTokens, EmisorDeTokensLocal>();
    }

    /// <summary>
    /// Registra el proveedor de almacenamiento que indique la configuración.
    /// </summary>
    /// <remarks>
    /// La decisión se toma al registrar, no en cada petición: el proveedor no
    /// cambia durante la vida del proceso y así el proveedor que no se usa ni
    /// siquiera se construye (en local no se crea ningún cliente de Azure).
    /// </remarks>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación, para leer el proveedor.</param>
    private static void AgregarAlmacenamiento(this IServiceCollection services, IConfiguration configuration)
    {
        string? proveedor = configuration[$"{OpcionesDeAlmacenamiento.Seccion}:{nameof(OpcionesDeAlmacenamiento.Proveedor)}"];

        if (string.Equals(proveedor, OpcionesDeAlmacenamiento.ProveedorLocal, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ServicioDeAlmacenLocal>();
            services.AddSingleton<IAlmacenDocumentos, AlmacenLocalDeDocumentos>();
            services.AddSingleton<IAnalizadorDeMalware, AnalizadorDeMalwareLocal>();
            return;
        }

        services.AddSingleton(sp =>
        {
            OpcionesDeAlmacenamiento opciones =
                sp.GetRequiredService<IOptions<OpcionesDeAlmacenamiento>>().Value;

            // Identidad administrada cuando hay punto de conexión; la cadena con
            // clave de cuenta queda reservada a Azurite.
            return string.IsNullOrWhiteSpace(opciones.UriServicio)
                ? new BlobServiceClient(opciones.CadenaDeConexion)
                : new BlobServiceClient(new Uri(opciones.UriServicio), new DefaultAzureCredential());
        });

        services.AddSingleton<IAlmacenDocumentos, BlobAlmacenDocumentos>();
        services.AddSingleton<IAnalizadorDeMalware, AnalizadorDeMalwareDiferido>();
        services.AddSingleton<InicializadorDeAlmacenamiento>();
    }

    /// <summary>
    /// Registra el cliente de la cola de avisos y el publicador: el real si las
    /// notificaciones están habilitadas, uno inactivo si no.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void AgregarNotificaciones(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            OpcionesDeNotificaciones opciones =
                sp.GetRequiredService<IOptions<OpcionesDeNotificaciones>>().Value;

            if (!string.IsNullOrWhiteSpace(opciones.UriServicioColas))
            {
                var uri = new Uri(new Uri(opciones.UriServicioColas), opciones.NombreDeCola);
                return new QueueClient(uri, new DefaultAzureCredential());
            }

            return new QueueClient(opciones.CadenaDeConexion, opciones.NombreDeCola);
        });

        services.AddSingleton<INotificationService>(sp =>
        {
            OpcionesDeNotificaciones opciones =
                sp.GetRequiredService<IOptions<OpcionesDeNotificaciones>>().Value;

            return opciones.Habilitado
                ? ActivatorUtilities.CreateInstance<PublicadorDeAvisosEnCola>(sp)
                : ActivatorUtilities.CreateInstance<PublicadorDeAvisosInactivo>(sp);
        });
    }

    /// <summary>
    /// Registra la política de carga con las extensiones y el tamaño máximo de
    /// la configuración.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    private static void AgregarPoliticaDeCarga(this IServiceCollection services)
    {
        // Sobrescribe el valor por defecto que registró la capa de aplicación:
        // en el contenedor, el último registro de un servicio es el que se resuelve.
        services.AddSingleton(sp =>
        {
            OpcionesDeCarga opciones = sp.GetRequiredService<IOptions<OpcionesDeCarga>>().Value;

            return new PoliticaDeCarga(
                opciones.ExtensionesPermitidas,
                TamanoArchivo.DesdeMegabytes(opciones.TamanoMaximoMegabytes));
        });
    }
}
