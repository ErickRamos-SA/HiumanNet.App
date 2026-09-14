using HuimanNet.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HuimanNet.Api.Seguridad;

/// <summary>
/// Nombres y registro de las políticas de autorización de la API.
/// </summary>
/// <remarks>
/// Las políticas deciden con el rol <b>vigente en la base de datos</b>, que
/// resuelve <see cref="MiddlewareDeUsuarioActual"/> antes de autorizar; no con
/// el rol que traía el token. Las políticas son una primera barrera gruesa: la
/// autorización fina por acción (permisos habilitados por el administrador) la
/// aplican siempre los casos de uso.
/// </remarks>
public static class PoliticasDeAutorizacion
{
    /// <summary>Nombre del rol de aplicación del usuario de empresa cliente.</summary>
    public const string RolClienteEmpresa = "ClienteEmpresa";

    /// <summary>Nombre del rol de aplicación del operador de nómina.</summary>
    public const string RolOperadorNomina = "OperadorNomina";

    /// <summary>Nombre del rol de aplicación del administrador.</summary>
    public const string RolAdministrador = "Administrador";

    /// <summary>Cualquier usuario registrado y activo del portal.</summary>
    public const string UsuarioDelPortal = "UsuarioDelPortal";

    /// <summary>Operador de nómina o administrador.</summary>
    public const string RolTransversal = "RolTransversal";

    /// <summary>Sólo administradores.</summary>
    public const string SoloAdministrador = "SoloAdministrador";

    /// <summary>Identidad de servicio que publica veredictos del antimalware.</summary>
    public const string ServicioDeEscaneo = "ServicioDeEscaneo";

    /// <summary>
    /// Registra las políticas.
    /// </summary>
    /// <param name="constructor">Constructor de autorización del anfitrión.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static AuthorizationBuilder AgregarPoliticasDeHuimanNet(this AuthorizationBuilder constructor)
    {
        ArgumentNullException.ThrowIfNull(constructor);

        return constructor
            .AddPolicy(UsuarioDelPortal, politica => politica
                .RequireAuthenticatedUser()
                .RequireAssertion(static c => UsuarioDe(c) is { EstaAutenticado: true }))
            .AddPolicy(RolTransversal, politica => politica
                .RequireAuthenticatedUser()
                .RequireAssertion(static c => UsuarioDe(c) is { EstaAutenticado: true } u
                    && u.Rol is RolUsuario.OperadorNomina or RolUsuario.Administrador))
            .AddPolicy(SoloAdministrador, politica => politica
                .RequireAuthenticatedUser()
                .RequireAssertion(static c => UsuarioDe(c) is { EstaAutenticado: true, Rol: RolUsuario.Administrador }))
            // El veredicto del antimalware lo publica una identidad de servicio,
            // no una persona: se exige el rol de aplicación en el token.
            .AddPolicy(ServicioDeEscaneo, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(RolAdministrador));
    }

    private static UsuarioActualDeHttpContext? UsuarioDe(AuthorizationHandlerContext contexto)
        => contexto.Resource is HttpContext http ? http.RequestServices.GetService<UsuarioActualDeHttpContext>() : null;
}
