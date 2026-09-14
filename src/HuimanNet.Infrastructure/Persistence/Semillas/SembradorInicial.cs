using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>
/// Carga los datos mínimos para operar: el catálogo de cálculo inicial (si la
/// base de datos no tiene ninguno) y un administrador (si no existe ninguno).
/// </summary>
/// <remarks>
/// Es seguro ejecutarlo en cada arranque y en cualquier entorno: sólo inserta
/// cuando la parte correspondiente del catálogo está <b>vacía</b>. Si el
/// administrador borra un concepto a propósito, no reaparece al reiniciar.
/// </remarks>
public sealed class SembradorInicial
{
    private readonly ICatalogoDeCalculoRepository _catalogos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IHasherDeContrasenas _hasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OpcionesDeIdentidad _identidad;
    private readonly TimeProvider _reloj;
    private readonly ILogger<SembradorInicial> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SembradorInicial"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="hasher">Derivación de contraseñas.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="identidad">Opciones de identidad.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public SembradorInicial(
        ICatalogoDeCalculoRepository catalogos,
        IUsuarioRepository usuarios,
        IHasherDeContrasenas hasher,
        IUnitOfWork unitOfWork,
        IOptions<OpcionesDeIdentidad> identidad,
        TimeProvider reloj,
        ILogger<SembradorInicial> logger)
    {
        ArgumentNullException.ThrowIfNull(identidad);
        _catalogos = catalogos;
        _usuarios = usuarios;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _identidad = identidad.Value;
        _reloj = reloj;
        _logger = logger;
    }

    /// <summary>
    /// Siembra lo que falte.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EjecutarAsync(CancellationToken cancellationToken = default)
    {
        await SembrarCatalogoAsync(cancellationToken);
        await GarantizarAdministradorAsync(cancellationToken);
    }

    private async Task SembrarCatalogoAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset ahora = _reloj.GetUtcNow();
        CatalogoInicial? catalogo = null;

        bool sinParametros = (await _catalogos.ListarParametrosAsync(null, cancellationToken)).Count == 0;
        bool sinTablas = (await _catalogos.ListarTablasAsync(null, cancellationToken)).Count == 0;
        bool sinConceptos = (await _catalogos.ListarConceptosAsync(null, cancellationToken)).Count == 0;
        bool sinExplicaciones = (await _catalogos.ListarExplicacionesAsync(null, null, cancellationToken)).Count == 0;

        if (!(sinParametros || sinTablas || sinConceptos || sinExplicaciones))
        {
            return;
        }

        catalogo = CatalogoInicial.Cargar();

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (sinParametros)
        {
            foreach (ParametroDeCalculo parametro in catalogo.ConstruirParametros(ahora))
            {
                await _catalogos.AgregarParametroAsync(parametro, cancellationToken);
            }
        }

        if (sinTablas)
        {
            foreach (TablaDeRangos tabla in catalogo.ConstruirTablas(ahora))
            {
                await _catalogos.AgregarTablaAsync(tabla, cancellationToken);
            }
        }

        if (sinConceptos)
        {
            foreach (ConceptoDeNomina concepto in catalogo.ConstruirConceptos(ahora))
            {
                await _catalogos.AgregarConceptoAsync(concepto, cancellationToken);
            }
        }

        if (sinExplicaciones)
        {
            foreach (ExplicacionDeCalculo explicacion in catalogo.ConstruirExplicaciones(ahora))
            {
                await _catalogos.AgregarExplicacionAsync(explicacion, cancellationToken);
            }
        }

        await transaccion.ConfirmarAsync(cancellationToken);

        _logger.LogInformation(
            "Catálogo de cálculo inicial cargado: {Parametros} parámetros, {Tablas} tablas, {Conceptos} conceptos.",
            sinParametros ? catalogo.Parametros.Count : 0,
            sinTablas ? catalogo.Tablas.Count : 0,
            sinConceptos ? catalogo.Conceptos.Count : 0);
    }

    private async Task GarantizarAdministradorAsync(CancellationToken cancellationToken)
    {
        OpcionesDeAdministradorInicial inicial = _identidad.AdministradorInicial;

        if (string.IsNullOrWhiteSpace(inicial.Correo))
        {
            return;
        }

        IReadOnlyList<Usuario> activos = await _usuarios.ListarAsync(null, incluirInactivos: false, cancellationToken);

        if (activos.Any(static u => u.Rol == RolUsuario.Administrador))
        {
            return;
        }

        if (await _usuarios.ObtenerPorCorreoAsync(inicial.Correo, cancellationToken) is not null)
        {
            _logger.LogWarning("El correo del administrador inicial ya pertenece a otro usuario; no se crea.");
            return;
        }

        Usuario administrador = string.IsNullOrWhiteSpace(inicial.Contrasena)
            ? Usuario.Crear(
                Usuario.PrefijoLocal + inicial.Correo.Trim().ToLowerInvariant(), inicial.NombreCompleto, inicial.Correo,
                RolUsuario.Administrador, null, _reloj.GetUtcNow())
            : Usuario.CrearLocal(
                inicial.NombreCompleto, inicial.Correo, RolUsuario.Administrador, null,
                _hasher.Hashear(inicial.Contrasena), _reloj.GetUtcNow());

        await _usuarios.AgregarAsync(administrador, cancellationToken);

        _logger.LogWarning(
            "Se creó el administrador inicial. Debe cambiar la contraseña en su primer acceso.");
    }
}
