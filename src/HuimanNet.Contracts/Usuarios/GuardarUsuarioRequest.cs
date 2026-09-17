using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Petición para crear o actualizar un usuario.
/// </summary>
/// <param name="NombreCompleto">Nombre.</param>
/// <param name="Correo">Correo; nombre de inicio de sesión en modo local.</param>
/// <param name="Rol">Rol.</param>
/// <param name="EmpresaId">Empresa principal, obligatoria para el rol de empresa cliente.</param>
/// <param name="Activo">Estado.</param>
/// <param name="Idioma">Idioma preferido.</param>
/// <param name="ContrasenaInicial">Contraseña inicial en modo local; se ignora al actualizar.</param>
/// <param name="Permisos">Permisos personalizados.</param>
/// <param name="EmpresasAdicionales">
/// Otras empresas a las que tiene acceso un usuario de empresa cliente; se
/// ignoran para los roles transversales, que operan sobre todas.
/// </param>
public sealed record GuardarUsuarioRequest(
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    bool Activo,
    Idioma Idioma,
    string? ContrasenaInicial,
    IReadOnlyList<PermisoDto> Permisos,
    IReadOnlyList<Guid>? EmpresasAdicionales = null);
