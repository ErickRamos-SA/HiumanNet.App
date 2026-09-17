using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Identidad de un usuario cuyas credenciales locales se validaron: lo que el
/// anfitrión web necesita para emitir su cookie de sesión.
/// </summary>
/// <param name="UsuarioId">Identificador local del usuario.</param>
/// <param name="IdentificadorExterno">Sujeto (<c>sub</c>) con el que el portal resolverá la identidad en cada circuito.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Rol">Rol funcional vigente.</param>
/// <param name="Idioma">Idioma preferido de la interfaz.</param>
/// <param name="RequiereCambioDeContrasena">Si debe cambiar la contraseña antes de operar.</param>
/// <remarks>
/// La cookie sólo identifica: el rol, las empresas y los permisos se vuelven a
/// leer de la base de datos en cada circuito.
/// </remarks>
public sealed record SesionLocalValidada(
    Guid UsuarioId,
    string IdentificadorExterno,
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Idioma Idioma,
    bool RequiereCambioDeContrasena);
