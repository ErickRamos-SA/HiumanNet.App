using System.Globalization;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.ValueObjects;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>
/// Carga empresas, empleados, períodos y usuarios de prueba para recorrer el
/// portal con los tres roles durante el desarrollo.
/// </summary>
/// <remarks>
/// Sólo corre cuando <c>SqlServer:SembrarDatosDePrueba</c> es <c>true</c>, valor
/// que únicamente habilita <c>appsettings.Development.json</c>; nunca debe
/// activarse en Azure.
/// <list type="bullet">
///   <item><description><b>Creatfor Demo</b>: empresa principal del cliente, con empleados IMSS (puro y mixto con complemento sindical), sindicato y honorarios.</description></item>
///   <item><description><b>Creatfor Servicios Demo</b>: empresa adicional del cliente.</description></item>
///   <item><description><b>Empresa Ajena Demo</b>: el cliente no la ve; sirve para comprobar el aislamiento.</description></item>
/// </list>
/// Cada empresa nueva recibe un período abierto del mes en curso, listo para
/// que el cliente suba sus incidencias.
/// <para>
/// Los usuarios <see cref="CorreoNomina"/> y <see cref="CorreoCliente"/> usan
/// la misma contraseña que el administrador: se les copia su <i>hash</i> en
/// cada arranque, de modo que si el administrador la cambia, los de prueba la
/// siguen. La contraseña nunca se maneja en claro.
/// </para>
/// <para>
/// Es idempotente: una empresa que ya existe (por RFC) no se modifica y de un
/// usuario que ya existe sólo se sincroniza la contraseña.
/// </para>
/// </remarks>
public sealed class SembradorDeDatosDePrueba
{
    /// <summary>Correo del usuario de prueba con rol de operador de nómina.</summary>
    public const string CorreoNomina = "nomina@huimannet.local";

    /// <summary>Correo del usuario de prueba con rol de empresa cliente.</summary>
    public const string CorreoCliente = "cliente@huimannet.local";

    private const decimal SalarioMinimoZonaB = 440.87m;
    private const decimal SdiMinimoZonaB = 465.03m;

    private static readonly DateOnly AltaDeContratos = new(2021, 1, 4);

    private static readonly ConfiguracionDeRazonSocial Configuracion = new(
        TipoDeServicio.Nomina, SubsidioAbsorbido: false, AplicaFaltasProporcionales: false,
        ModalidadDeComision.SobreCosto, 0.08m, ZonaIsn.SegunZonaDelTrabajador, 0.16m, 0m, null);

    /// <summary>Empresa principal del usuario cliente de prueba.</summary>
    private static readonly EmpresaDePrueba Principal = new(
        "Creatfor Demo",
        "CDE210104AB1",
        [
            new("Creatfor Imagen y Ventas Demo", "CIV210104DE1", "Y5239554107"),
            new("Sindicato Progreso Demo", "SPD210104DE1", null),
            new("Consultores Asociados Demo", "CAD210104DE1", null),
        ],
        [
            new("1", "Stephany", "Sarmiento", "Chong", 0, EsquemaDePago.Imss, "917", "Asesora",
                22000m, SalarioMinimoZonaB, SdiMinimoZonaB, ZonaSalarioMinimo.B, PagaComplemento: true),
            new("9", "Luis Alberto", "Melchor", "Garcia", 0, EsquemaDePago.Imss, "1511", "Asesor",
                10000m, SalarioMinimoZonaB, SdiMinimoZonaB, ZonaSalarioMinimo.B, PagaComplemento: true),
            new("12", "Ana Karen", "Lopez", "Ruiz", 0, EsquemaDePago.Imss, "1520", "Auxiliar administrativa",
                3500m, 500m, 522.6m, ZonaSalarioMinimo.A),
            new("15", "Jorge", "Ramirez", "Soto", 1, EsquemaDePago.Sindicato, "2001", "Operador",
                6000m, 0m, 0m, ZonaSalarioMinimo.B),
            new("20", "Mariana", "Torres", "Vega", 2, EsquemaDePago.Honorarios, "3001", "Consultora",
                12000m, 0m, 0m, ZonaSalarioMinimo.B, HonorariosConIva: true),
        ]);

    /// <summary>Empresa adicional del usuario cliente de prueba.</summary>
    private static readonly EmpresaDePrueba Adicional = new(
        "Creatfor Servicios Demo",
        "CSD210104AB1",
        [new("Creatfor Servicios Integrales Demo", "CSI210104DE1", "Y5239554115")],
        [
            new("1", "Roberto", "Diaz", "Mora", 0, EsquemaDePago.Imss, "101", "Supervisor",
                9000m, SalarioMinimoZonaB, SdiMinimoZonaB, ZonaSalarioMinimo.B, PagaComplemento: true),
            new("2", "Laura", "Castillo", "Pena", 0, EsquemaDePago.Imss, "102", "Recepcionista",
                3150m, 450m, 465.03m, ZonaSalarioMinimo.B),
        ]);

    /// <summary>Empresa a la que el usuario cliente de prueba no tiene acceso.</summary>
    private static readonly EmpresaDePrueba Ajena = new(
        "Empresa Ajena Demo",
        "EAD210104AB1",
        [new("Ajena Operadora Demo", "AOD210104DE1", "Y5239554123")],
        [
            new("1", "Pedro", "Hernandez", "Luna", 0, EsquemaDePago.Imss, "501", "Almacenista",
                8000m, SalarioMinimoZonaB, SdiMinimoZonaB, ZonaSalarioMinimo.B, PagaComplemento: true),
        ]);

    private static readonly EmpresaDePrueba[] Empresas = [Principal, Adicional, Ajena];

    private readonly IEmpresaRepository _empresas;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly IEmpleadoRepository _empleados;
    private readonly IPeriodoRepository _periodos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OpcionesDeIdentidad _identidad;
    private readonly TimeProvider _reloj;
    private readonly ILogger<SembradorDeDatosDePrueba> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SembradorDeDatosDePrueba"/>.
    /// </summary>
    /// <param name="empresas">Repositorio de empresas.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="identidad">Opciones de identidad, para reconocer al administrador inicial.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public SembradorDeDatosDePrueba(
        IEmpresaRepository empresas,
        IRazonSocialRepository razonesSociales,
        IEmpleadoRepository empleados,
        IPeriodoRepository periodos,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork,
        IOptions<OpcionesDeIdentidad> identidad,
        TimeProvider reloj,
        ILogger<SembradorDeDatosDePrueba> logger)
    {
        ArgumentNullException.ThrowIfNull(identidad);
        _empresas = empresas;
        _razonesSociales = razonesSociales;
        _empleados = empleados;
        _periodos = periodos;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
        _identidad = identidad.Value;
        _reloj = reloj;
        _logger = logger;
    }

    /// <summary>
    /// Crea los datos de prueba que falten y sincroniza la contraseña de los usuarios de prueba.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EjecutarAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset ahora = _reloj.GetUtcNow();

        Dictionary<string, Guid> existentes = (await _empresas.ListarAsync(soloActivas: false, cancellationToken))
            .ToDictionary(static e => e.IdentificadorFiscal, static e => e.Id, StringComparer.OrdinalIgnoreCase);

        string? hash = await HashDelAdministradorAsync(cancellationToken);
        Usuario? nomina = await _usuarios.ObtenerPorCorreoAsync(CorreoNomina, cancellationToken);
        Usuario? cliente = await _usuarios.ObtenerPorCorreoAsync(CorreoCliente, cancellationToken);

        try
        {
            await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

            int empresasNuevas = 0;

            foreach (EmpresaDePrueba empresa in Empresas)
            {
                if (!existentes.ContainsKey(empresa.Rfc))
                {
                    existentes[empresa.Rfc] = await CrearEmpresaAsync(empresa, ahora, cancellationToken);
                    empresasNuevas++;
                }
            }

            await GarantizarUsuarioAsync(
                nomina, "Operador de nómina (prueba)", CorreoNomina, RolUsuario.OperadorNomina,
                null, [], hash, ahora, cancellationToken);

            await GarantizarUsuarioAsync(
                cliente, "Cliente Creatfor (prueba)", CorreoCliente, RolUsuario.ClienteEmpresa,
                existentes[Principal.Rfc], [existentes[Adicional.Rfc]], hash, ahora, cancellationToken);

            await transaccion.ConfirmarAsync(cancellationToken);

            if (empresasNuevas > 0 || nomina is null || cliente is null)
            {
                _logger.LogWarning(
                    "Datos de prueba cargados: {Empresas} empresas nuevas y los usuarios {Nomina} y {Cliente}. " +
                    "Nunca habilite 'SqlServer:SembrarDatosDePrueba' fuera de desarrollo.",
                    empresasNuevas, CorreoNomina, CorreoCliente);
            }
        }
        catch (SqlException excepcion) when (excepcion.Number is 2601 or 2627)
        {
            // La web y la API arrancan a la vez desde Visual Studio: si ambas
            // siembran al mismo tiempo, los índices únicos dejan pasar sólo a una.
            _logger.LogInformation("Otro proceso cargó los datos de prueba al mismo tiempo; se conservan los suyos.");
        }
    }

    /// <summary>
    /// Obtiene el <i>hash</i> de la contraseña del administrador, dando
    /// preferencia al administrador inicial de la configuración.
    /// </summary>
    /// <returns>El <i>hash</i>, o <c>null</c> si ningún administrador activo tiene contraseña local (modo Entra).</returns>
    private async Task<string?> HashDelAdministradorAsync(CancellationToken cancellationToken)
    {
        string correoInicial = _identidad.AdministradorInicial.Correo;

        IReadOnlyList<Usuario> activos = await _usuarios.ListarAsync(null, incluirInactivos: false, cancellationToken);

        Usuario? administrador = activos
            .Where(static u => u.Rol == RolUsuario.Administrador && u.TieneContrasenaLocal)
            .OrderByDescending(u => string.Equals(u.Correo, correoInicial, StringComparison.OrdinalIgnoreCase))
            .ThenBy(static u => u.FechaAlta)
            .FirstOrDefault();

        if (administrador is null)
        {
            _logger.LogWarning(
                "Ningún administrador activo tiene contraseña local: los usuarios de prueba se crean sin contraseña.");
        }

        return administrador?.HashContrasena;
    }

    private async Task GarantizarUsuarioAsync(
        Usuario? existente,
        string nombreCompleto,
        string correo,
        RolUsuario rol,
        Guid? empresaId,
        IReadOnlyList<Guid> empresasAdicionales,
        string? hash,
        DateTimeOffset ahora,
        CancellationToken cancellationToken)
    {
        if (existente is null)
        {
            Usuario nuevo = Usuario.Crear(
                Usuario.PrefijoLocal + correo, nombreCompleto, correo, rol, empresaId, ahora);

            if (hash is not null)
            {
                nuevo.EstablecerContrasena(hash, requiereCambio: false);
            }

            nuevo.AsignarEmpresasAdicionales(empresasAdicionales);
            await _usuarios.AgregarAsync(nuevo, cancellationToken);
            return;
        }

        // Del usuario existente sólo se sincroniza la contraseña: el rol y las
        // empresas pueden haberse cambiado a propósito desde el portal.
        if (hash is not null && (existente.HashContrasena != hash || existente.RequiereCambioDeContrasena))
        {
            existente.EstablecerContrasena(hash, requiereCambio: false);
            await _usuarios.ActualizarAsync(existente, cancellationToken);
        }
    }

    private async Task<Guid> CrearEmpresaAsync(EmpresaDePrueba datos, DateTimeOffset ahora, CancellationToken cancellationToken)
    {
        Empresa empresa = Empresa.Crear(datos.RazonSocial, datos.Rfc, ahora);
        await _empresas.AgregarAsync(empresa, cancellationToken);

        var razones = new List<Guid>(datos.Razones.Count);

        foreach (RazonDePrueba razon in datos.Razones)
        {
            RazonSocial razonSocial = RazonSocial.Crear(
                empresa.Id, razon.Nombre, razon.Rfc, razon.RegistroPatronal, ZonaSalarioMinimo.B, Configuracion, "BBVA", ahora);

            await _razonesSociales.AgregarAsync(razonSocial, cancellationToken);
            razones.Add(razonSocial.Id);
        }

        foreach (EmpleadoDePrueba e in datos.Empleados)
        {
            Empleado empleado = Empleado.Crear(
                empresa.Id, e.Clave, new DatosPersonales(e.Nombre, e.Paterno, e.Materno, null, null, null, null, null, null), ahora);

            await _empleados.AgregarAsync(empleado, cancellationToken);

            Contrato contrato = Contrato.Crear(
                empleado.Id, empresa.Id, razones[e.Razon], e.Esquema, e.Noi, e.Puesto, "Operación", "Presencial",
                new CondicionesDeContrato(
                    e.SueldoReal, e.SalarioDiarioFiscal, e.Sdi, e.Zona, CreditoInfonavit.Ninguno, 0m, 0m, 0m, 0m, 0m,
                    e.HonorariosConIva, e.PagaComplemento),
                AltaDeContratos, ahora);

            await _empleados.AgregarContratoAsync(contrato, cancellationToken);
        }

        PeriodoCalendario calendario = PeriodoCalendario.Crear(ahora.Year, ahora.Month, 1);
        string mes = CultureInfo.GetCultureInfo("es-MX").DateTimeFormat.GetMonthName(ahora.Month);

        await _periodos.AgregarAsync(
            PeriodoCarga.Abrir(empresa.Id, calendario, $"Semana 1 de {mes} {ahora.Year} (prueba)", ahora, ahora.AddDays(7)),
            cancellationToken);

        return empresa.Id;
    }

    private sealed record EmpresaDePrueba(
        string RazonSocial, string Rfc, IReadOnlyList<RazonDePrueba> Razones, IReadOnlyList<EmpleadoDePrueba> Empleados);

    private sealed record RazonDePrueba(string Nombre, string Rfc, string? RegistroPatronal);

    /// <summary>Empleado con un único contrato; <see cref="Razon"/> es el índice en la lista de razones de su empresa.</summary>
    private sealed record EmpleadoDePrueba(
        string Clave,
        string Nombre,
        string Paterno,
        string? Materno,
        int Razon,
        EsquemaDePago Esquema,
        string Noi,
        string Puesto,
        decimal SueldoReal,
        decimal SalarioDiarioFiscal,
        decimal Sdi,
        ZonaSalarioMinimo Zona,
        bool PagaComplemento = false,
        bool HonorariosConIva = false);
}
