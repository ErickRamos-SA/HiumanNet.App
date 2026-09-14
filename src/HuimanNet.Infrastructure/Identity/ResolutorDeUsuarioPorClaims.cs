using System.Globalization;
using System.Security.Claims;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Identity;

/// <summary>
/// Traduce el <see cref="ClaimsPrincipal"/> de una petición (token de Entra,
/// token local o cookie) a un <see cref="Usuario"/> de HuimanNet.
/// </summary>
/// <remarks>
/// <b>La base de datos es la fuente de verdad</b> del rol, la empresa y los
/// permisos: los gestiona el administrador desde el portal. El proveedor de
/// identidad sólo autentica.
/// <list type="bullet">
///   <item><description>Si el usuario existe, se usa tal cual; si está inactivo, se rechaza.</description></item>
///   <item><description>En modo Entra, un usuario pre-aprovisionado por el administrador se enlaza por correo en su primer acceso.</description></item>
///   <item><description>En modo Entra, un usuario desconocido cuyo token trae un rol de aplicación se aprovisiona <i>just-in-time</i> (compatibilidad con la V1).</description></item>
///   <item><description>En modo local, un usuario desconocido se rechaza: sólo el administrador da de alta.</description></item>
/// </list>
/// Lo comparten la API y la web, de modo que un mismo token se traduce siempre
/// a la misma identidad y al mismo rol.
/// </remarks>
public sealed class ResolutorDeUsuarioPorClaims
{
    private readonly IUsuarioRepository _usuarios;
    private readonly OpcionesDeIdentidad _opciones;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ResolutorDeUsuarioPorClaims> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ResolutorDeUsuarioPorClaims"/>.
    /// </summary>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="opciones">Opciones de identidad.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ResolutorDeUsuarioPorClaims(
        IUsuarioRepository usuarios,
        IOptions<OpcionesDeIdentidad> opciones,
        TimeProvider reloj,
        ILogger<ResolutorDeUsuarioPorClaims> logger)
    {
        ArgumentNullException.ThrowIfNull(usuarios);
        ArgumentNullException.ThrowIfNull(opciones);

        _usuarios = usuarios;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    /// <summary>
    /// Resuelve —y, cuando procede, enlaza o aprovisiona— el usuario de la petición.
    /// </summary>
    /// <param name="principal">Identidad autenticada de la petición o del circuito.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario local correspondiente.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el token no trae identificador, si el usuario no está
    /// registrado ni puede aprovisionarse, o si está desactivado.
    /// </exception>
    public async Task<Usuario> ResolverAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string identificadorExterno = LeerIdentificadorExterno(principal);
        Usuario? usuario = await _usuarios.ObtenerPorIdentificadorExternoAsync(identificadorExterno, cancellationToken);

        if (usuario is null && !_opciones.EsLocal)
        {
            usuario = await EnlazarPreaprovisionadoAsync(principal, identificadorExterno, cancellationToken);
        }

        if (usuario is null)
        {
            return await AprovisionarAsync(principal, identificadorExterno, cancellationToken);
        }

        if (!usuario.Activo)
        {
            throw new AccesoNoAutorizadoException($"El usuario '{usuario.Id}' está desactivado en el portal.");
        }

        return usuario;
    }

    private async Task<Usuario?> EnlazarPreaprovisionadoAsync(
        ClaimsPrincipal principal, string identificadorExterno, CancellationToken cancellationToken)
    {
        string? correo = LeerCorreo(principal);

        if (correo is null)
        {
            return null;
        }

        Usuario? candidato = await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken);

        if (candidato is null
            || !candidato.IdentificadorExterno.StartsWith(Usuario.PrefijoLocal, StringComparison.Ordinal)
            || candidato.TieneContrasenaLocal)
        {
            return null;
        }

        candidato.VincularIdentidadExterna(identificadorExterno);
        await _usuarios.ActualizarAsync(candidato, cancellationToken);

        _logger.LogInformation("Usuario {UsuarioId} enlazado con su identidad de Entra en su primer acceso.", candidato.Id);
        return candidato;
    }

    private async Task<Usuario> AprovisionarAsync(
        ClaimsPrincipal principal, string identificadorExterno, CancellationToken cancellationToken)
    {
        RolUsuario? rol = _opciones.EsLocal ? null : LeerRol(principal);

        if (rol is null)
        {
            throw new AccesoNoAutorizadoException(
                "El usuario no está registrado en el portal. Solicite el alta al administrador.");
        }

        Guid? empresaId = LeerEmpresa(principal, rol.Value);
        string nombre = Valor(principal, "name") ?? Valor(principal, ClaimTypes.Name) ?? "(sin nombre)";
        string correo = LeerCorreo(principal) ?? $"{identificadorExterno}@sin-correo.local";

        Usuario usuario = Usuario.Crear(identificadorExterno, nombre, correo, rol.Value, empresaId, _reloj.GetUtcNow());
        await _usuarios.AgregarAsync(usuario, cancellationToken);

        _logger.LogInformation("Usuario {UsuarioId} aprovisionado con rol {Rol} en su primer acceso.", usuario.Id, rol);
        return usuario;
    }

    /// <summary>
    /// Lee el primer valor no vacío de un <i>claim</i>.
    /// </summary>
    /// <remarks>
    /// No se usa <c>FindFirstValue</c> porque es una extensión de ASP.NET Core y
    /// esta capa no debe depender del anfitrión web.
    /// </remarks>
    private static string? Valor(ClaimsPrincipal principal, string tipo)
    {
        string? valor = principal.FindFirst(tipo)?.Value;
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static string LeerIdentificadorExterno(ClaimsPrincipal principal)
        => Valor(principal, "oid")
           ?? Valor(principal, "http://schemas.microsoft.com/identity/claims/objectidentifier")
           ?? Valor(principal, "sub")
           ?? Valor(principal, ClaimTypes.NameIdentifier)
           ?? throw new AccesoNoAutorizadoException("El token no contiene un identificador de sujeto ('oid' o 'sub').");

    private static string? LeerCorreo(ClaimsPrincipal principal)
        => Valor(principal, "preferred_username") ?? Valor(principal, "email") ?? Valor(principal, ClaimTypes.Email);

    private RolUsuario? LeerRol(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(_opciones.RolAdministrador))
        {
            return RolUsuario.Administrador;
        }

        if (principal.IsInRole(_opciones.RolOperadorNomina))
        {
            return RolUsuario.OperadorNomina;
        }

        return principal.IsInRole(_opciones.RolClienteEmpresa) ? RolUsuario.ClienteEmpresa : null;
    }

    private Guid? LeerEmpresa(ClaimsPrincipal principal, RolUsuario rol)
    {
        string? valor = Valor(principal, _opciones.ClaimDeEmpresa);

        if (string.IsNullOrWhiteSpace(valor))
        {
            return rol == RolUsuario.ClienteEmpresa
                ? throw new AccesoNoAutorizadoException(
                    $"El token de un usuario de empresa cliente debe traer el claim '{_opciones.ClaimDeEmpresa}'.")
                : null;
        }

        return Guid.TryParse(valor, CultureInfo.InvariantCulture, out Guid empresaId)
            ? empresaId
            : throw new AccesoNoAutorizadoException($"El claim '{_opciones.ClaimDeEmpresa}' no contiene un identificador válido.");
    }
}
