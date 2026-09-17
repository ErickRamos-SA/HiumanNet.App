using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Proyección de lectura de un usuario para la administración.
/// </summary>
/// <param name="Id">Identificador local.</param>
/// <param name="NombreCompleto">Nombre.</param>
/// <param name="Correo">Correo.</param>
/// <param name="Rol">Rol.</param>
/// <param name="EmpresaId">Empresa, si aplica.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa, si aplica.</param>
/// <param name="Activo">Estado.</param>
/// <param name="Idioma">Idioma preferido.</param>
/// <param name="TieneContrasenaLocal">Si puede iniciar sesión con contraseña.</param>
/// <param name="RequiereCambioDeContrasena">Si debe cambiar la contraseña.</param>
/// <param name="FechaAlta">Fecha de alta, en UTC.</param>
/// <param name="Permisos">Permisos personalizados.</param>
/// <param name="AccionesEfectivas">Acciones habilitadas tras combinar rol y permisos.</param>
/// <param name="EmpresasAdicionales">Otras empresas, además de la principal, a las que tiene acceso un usuario de empresa cliente.</param>
public sealed record UsuarioDto(
    Guid Id,
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    string? EmpresaRazonSocial,
    bool Activo,
    Idioma Idioma,
    bool TieneContrasenaLocal,
    bool RequiereCambioDeContrasena,
    DateTimeOffset FechaAlta,
    IReadOnlyList<PermisoDto> Permisos,
    IReadOnlyList<AccionDelSistema> AccionesEfectivas,
    IReadOnlyList<Guid> EmpresasAdicionales);
