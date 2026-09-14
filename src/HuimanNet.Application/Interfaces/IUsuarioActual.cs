using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Identidad efectiva del usuario que origina la petición en curso.
/// </summary>
/// <remarks>
/// La implementa cada anfitrión: la API a partir del <c>ClaimsPrincipal</c> del
/// token y Blazor Server a partir del estado de autenticación del circuito.
/// <para>
/// <b>Es la única fuente autorizada de la empresa del solicitante.</b> Ningún
/// caso de uso debe tomar la empresa de la petición sin pasarla por
/// <see cref="Domain.Services.PoliticaDeAcceso.ResolverEmpresaObjetivo(RolUsuario, Nullable{Guid}, IReadOnlyCollection{Guid}, Nullable{Guid})"/>.
/// </para>
/// </remarks>
public interface IUsuarioActual
{
    /// <summary>
    /// Obtiene el identificador local del usuario.
    /// </summary>
    /// <value>Clave con la que se firman los asientos de auditoría.</value>
    Guid UsuarioId { get; }

    /// <summary>
    /// Obtiene el nombre para mostrar del usuario.
    /// </summary>
    /// <value>Texto tomado del token de identidad o del registro local.</value>
    string NombreCompleto { get; }

    /// <summary>
    /// Obtiene la dirección de correo del usuario.
    /// </summary>
    /// <value>Destino de las notificaciones del portal.</value>
    string Correo { get; }

    /// <summary>
    /// Obtiene el rol funcional del usuario.
    /// </summary>
    /// <value>Resuelto en el servidor a partir del registro local o de los <i>claims</i> de rol.</value>
    RolUsuario Rol { get; }

    /// <summary>
    /// Obtiene la empresa principal del usuario.
    /// </summary>
    /// <value>
    /// La empresa con la que opera por omisión; <c>null</c> para el operador de
    /// nómina y el administrador.
    /// </value>
    Guid? EmpresaId { get; }

    /// <summary>
    /// Obtiene las empresas en las que opera el usuario.
    /// </summary>
    /// <value>
    /// Para la empresa cliente, su empresa principal primero y después las
    /// adicionales; vacía para el operador de nómina y el administrador, que
    /// operan sobre todas.
    /// </value>
    IReadOnlyList<Guid> Empresas { get; }

    /// <summary>
    /// Obtiene los permisos personalizados que ajustan los del rol.
    /// </summary>
    /// <value>Lista de sólo lectura; vacía si no hay ajustes.</value>
    IReadOnlyList<PermisoDeUsuario> Permisos { get; }

    /// <summary>
    /// Obtiene el idioma preferido del usuario.
    /// </summary>
    /// <value>Español por defecto.</value>
    Idioma Idioma { get; }

    /// <summary>
    /// Indica si el usuario debe cambiar su contraseña local antes de operar.
    /// </summary>
    /// <value><c>true</c> tras un alta o un restablecimiento.</value>
    bool RequiereCambioDeContrasena { get; }

    /// <summary>
    /// Obtiene la dirección IP de origen de la petición.
    /// </summary>
    /// <value>Cadena con la IP del cliente, o <c>null</c> si no está disponible.</value>
    string? DireccionIp { get; }

    /// <summary>
    /// Indica si hay un usuario autenticado en la petición actual.
    /// </summary>
    /// <value><c>false</c> en peticiones anónimas.</value>
    bool EstaAutenticado { get; }
}
