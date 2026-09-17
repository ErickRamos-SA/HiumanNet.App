using System.Globalization;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Traduce una <see cref="IdentidadAutenticada"/> a un <see cref="Usuario"/>
/// del portal y, cuando procede, lo enlaza o lo da de alta.
/// </summary>
/// <remarks>
/// <b>La base de datos es la fuente de verdad</b> del rol, la empresa y los
/// permisos: el proveedor de identidad sólo autentica.
/// <list type="bullet">
///   <item><description>Si el usuario existe, se usa tal cual; si está inactivo, se rechaza.</description></item>
///   <item><description>Con una cuenta externa, un usuario dado de alta por correo que aún no ha entrado se enlaza en su primer acceso.</description></item>
///   <item><description>Con una cuenta externa, un usuario desconocido cuyo token trae un rol del portal se da de alta en su primer acceso.</description></item>
///   <item><description>Con cuentas locales, un usuario desconocido se rechaza: sólo el administrador da de alta.</description></item>
/// </list>
/// El enlace y el alta automática quedan en la bitácora, como cualquier otra
/// modificación de usuarios. Lo comparten la API y la web, de modo que un
/// mismo token se traduce siempre a la misma identidad.
/// </remarks>
public sealed class ResolutorDeUsuarioAutenticado
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ResolutorDeUsuarioAutenticado> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ResolutorDeUsuarioAutenticado"/>.
    /// </summary>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="auditoria">Repositorio de la bitácora.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ResolutorDeUsuarioAutenticado(
        IUsuarioRepository usuarios,
        IAuditoriaRepository auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj,
        ILogger<ResolutorDeUsuarioAutenticado> logger)
    {
        _usuarios = usuarios;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
        _logger = logger;
    }

    /// <summary>
    /// Resuelve —y, cuando procede, enlaza o da de alta— el usuario de una identidad.
    /// </summary>
    /// <param name="identidad">Identidad autenticada.</param>
    /// <param name="direccionIp">Dirección IP de la petición, para la bitácora, si se conoce.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario del portal.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el usuario no está registrado ni puede darse de alta, o si está desactivado.
    /// </exception>
    public async Task<Usuario> ResolverAsync(
        IdentidadAutenticada identidad, string? direccionIp, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identidad);

        Usuario? usuario = await _usuarios.ObtenerPorIdentificadorExternoAsync(identidad.IdentificadorExterno, cancellationToken);

        if (usuario is null && identidad.EsCuentaExterna)
        {
            usuario = await EnlazarPreaprovisionadoAsync(identidad, direccionIp, cancellationToken);
        }

        if (usuario is null)
        {
            return await AprovisionarAsync(identidad, direccionIp, cancellationToken);
        }

        return usuario.Activo
            ? usuario
            : throw new AccesoNoAutorizadoException($"El usuario '{usuario.Id}' está desactivado en el portal.");
    }

    /// <summary>
    /// Vincula con su identidad externa a un usuario que el administrador dio
    /// de alta por correo y que todavía no ha entrado ni tiene contraseña local.
    /// </summary>
    /// <param name="identidad">Identidad autenticada.</param>
    /// <param name="direccionIp">Dirección IP de la petición, si se conoce.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario vinculado, o <c>null</c> si no hay uno pendiente de enlazar con ese correo.</returns>
    private async Task<Usuario?> EnlazarPreaprovisionadoAsync(
        IdentidadAutenticada identidad, string? direccionIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identidad.Correo))
        {
            return null;
        }

        Usuario? candidato = await _usuarios.ObtenerPorCorreoAsync(identidad.Correo, cancellationToken);

        if (candidato is null
            || !candidato.IdentificadorExterno.StartsWith(Usuario.PrefijoLocal, StringComparison.Ordinal)
            || candidato.TieneContrasenaLocal)
        {
            return null;
        }

        candidato.VincularIdentidadExterna(identidad.IdentificadorExterno);

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _usuarios.ActualizarAsync(candidato, cancellationToken);
        await AuditarAsync(candidato, "vinculacion-identidad-externa", direccionIp, cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        _logger.LogInformation("Usuario {UsuarioId} enlazado con su identidad externa en su primer acceso.", candidato.Id);
        return candidato;
    }

    /// <summary>
    /// Da de alta en su primer acceso a un usuario externo que trae un rol del portal.
    /// </summary>
    /// <param name="identidad">Identidad autenticada.</param>
    /// <param name="direccionIp">Dirección IP de la petición, si se conoce.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario creado.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza con cuentas locales, si la identidad no trae un rol del portal
    /// o si a un usuario de empresa cliente le falta una empresa válida.
    /// </exception>
    private async Task<Usuario> AprovisionarAsync(
        IdentidadAutenticada identidad, string? direccionIp, CancellationToken cancellationToken)
    {
        if (!identidad.EsCuentaExterna || identidad.RolDeAplicacion is not { } rol)
        {
            throw new AccesoNoAutorizadoException(
                "El usuario no está registrado en el portal. Solicite el alta al administrador.");
        }

        Guid? empresaId = EmpresaDe(identidad, rol);
        string nombre = string.IsNullOrWhiteSpace(identidad.NombreCompleto) ? "(sin nombre)" : identidad.NombreCompleto;
        string correo = string.IsNullOrWhiteSpace(identidad.Correo)
            ? $"{identidad.IdentificadorExterno}@sin-correo.local"
            : identidad.Correo;

        Usuario usuario = Usuario.Crear(identidad.IdentificadorExterno, nombre, correo, rol, empresaId, _reloj.GetUtcNow());

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _usuarios.AgregarAsync(usuario, cancellationToken);
        await AuditarAsync(usuario, string.Create(CultureInfo.InvariantCulture, $"alta-automatica; rol={rol}"), direccionIp, cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        _logger.LogInformation("Usuario {UsuarioId} dado de alta con rol {Rol} en su primer acceso.", usuario.Id, rol);
        return usuario;
    }

    /// <summary>Interpreta la empresa indicada por el proveedor.</summary>
    /// <param name="identidad">Identidad autenticada.</param>
    /// <param name="rol">Rol del usuario; sólo la empresa cliente la exige.</param>
    /// <returns>La empresa, o <c>null</c> si el rol no la necesita y no viene.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si a un usuario de empresa cliente le falta la empresa o no es un identificador válido.
    /// </exception>
    private static Guid? EmpresaDe(IdentidadAutenticada identidad, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(identidad.EmpresaIndicada))
        {
            return rol == RolUsuario.ClienteEmpresa
                ? throw new AccesoNoAutorizadoException("La identidad de un usuario de empresa cliente debe indicar su empresa.")
                : null;
        }

        return Guid.TryParse(identidad.EmpresaIndicada, CultureInfo.InvariantCulture, out Guid empresaId)
            ? empresaId
            : throw new AccesoNoAutorizadoException("La empresa indicada por el proveedor de identidad no es un identificador válido.");
    }

    /// <summary>Registra en la bitácora el enlace o el alta automática de un usuario.</summary>
    /// <param name="usuario">Usuario afectado, que es también quien actúa.</param>
    /// <param name="detalle">Operación realizada.</param>
    /// <param name="direccionIp">Dirección IP de la petición, si se conoce.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al registrar el asiento.</returns>
    private Task AuditarAsync(Usuario usuario, string detalle, string? direccionIp, CancellationToken cancellationToken)
        => _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.AdministracionDeUsuario, usuario.Id, usuario.EmpresaId, nameof(Usuario), usuario.Id,
                _reloj.GetUtcNow(), detalle, direccionIp),
            cancellationToken);
}
