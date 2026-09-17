using System.Security.Claims;
using HuimanNet.Application.Usuarios;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Identity;

/// <summary>
/// Lee del <see cref="ClaimsPrincipal"/> de una petición (token de Entra,
/// token local o cookie) la <see cref="IdentidadAutenticada"/> que la capa de
/// aplicación traduce a un usuario.
/// </summary>
/// <remarks>
/// Sólo interpreta claims: nombres de claim, roles de aplicación configurados
/// y modo de identidad. Qué hacer con un usuario desconocido lo decide
/// <see cref="ResolutorDeUsuarioAutenticado"/>, que además lo deja en la bitácora.
/// </remarks>
public sealed class LectorDeIdentidadPorClaims
{
    private readonly OpcionesDeIdentidad _opciones;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LectorDeIdentidadPorClaims"/>.
    /// </summary>
    /// <param name="opciones">Opciones de identidad.</param>
    public LectorDeIdentidadPorClaims(IOptions<OpcionesDeIdentidad> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    /// <summary>
    /// Lee la identidad autenticada de la petición o del circuito.
    /// </summary>
    /// <param name="principal">Identidad autenticada.</param>
    /// <returns>La identidad, lista para resolverse.</returns>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el token no trae identificador de sujeto.</exception>
    public IdentidadAutenticada Leer(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return new IdentidadAutenticada(
            LeerIdentificadorExterno(principal),
            EsCuentaExterna: !_opciones.EsLocal,
            LeerCorreo(principal),
            Valor(principal, "name") ?? Valor(principal, ClaimTypes.Name),
            _opciones.EsLocal ? null : LeerRol(principal),
            Valor(principal, _opciones.ClaimDeEmpresa));
    }

    /// <summary>
    /// Lee el primer valor no vacío de un <i>claim</i>.
    /// </summary>
    /// <remarks>
    /// No se usa <c>FindFirstValue</c> porque es una extensión de ASP.NET Core y
    /// esta capa no debe depender del anfitrión web.
    /// </remarks>
    /// <param name="principal">Identidad del token.</param>
    /// <param name="tipo">Tipo de claim.</param>
    /// <returns>El valor, o <c>null</c> si no existe o está vacío.</returns>
    private static string? Valor(ClaimsPrincipal principal, string tipo)
    {
        string? valor = principal.FindFirst(tipo)?.Value;
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    /// <summary>Lee el identificador del sujeto: <c>oid</c> de Entra o, en su defecto, <c>sub</c>.</summary>
    /// <param name="principal">Identidad del token.</param>
    /// <returns>El identificador externo.</returns>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el token no trae ninguno.</exception>
    private static string LeerIdentificadorExterno(ClaimsPrincipal principal)
        => Valor(principal, "oid")
           ?? Valor(principal, "http://schemas.microsoft.com/identity/claims/objectidentifier")
           ?? Valor(principal, "sub")
           ?? Valor(principal, ClaimTypes.NameIdentifier)
           ?? throw new AccesoNoAutorizadoException("El token no contiene un identificador de sujeto ('oid' o 'sub').");

    /// <summary>Lee el correo del token.</summary>
    /// <param name="principal">Identidad del token.</param>
    /// <returns>El correo, o <c>null</c> si el token no lo trae.</returns>
    private static string? LeerCorreo(ClaimsPrincipal principal)
        => Valor(principal, "preferred_username") ?? Valor(principal, "email") ?? Valor(principal, ClaimTypes.Email);

    /// <summary>Traduce el rol de aplicación de Entra al rol del portal.</summary>
    /// <param name="principal">Identidad del token.</param>
    /// <returns>El rol de mayor privilegio que traiga el token, o <c>null</c> si no trae ninguno.</returns>
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
}
