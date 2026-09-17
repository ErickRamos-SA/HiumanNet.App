using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Datos de una fila de <c>dbo.Usuarios</c>, pendientes de unir con sus permisos
/// y empresas adicionales.
/// </summary>
/// <param name="Id">Identificador local.</param>
/// <param name="IdentificadorExterno">Identificador en el proveedor de identidad.</param>
/// <param name="NombreCompleto">Nombre.</param>
/// <param name="Correo">Correo.</param>
/// <param name="Rol">Rol.</param>
/// <param name="EmpresaId">Empresa principal, si aplica.</param>
/// <param name="Activo">Estado.</param>
/// <param name="FechaAlta">Fecha de alta.</param>
/// <param name="HashContrasena">Hash de contraseña local.</param>
/// <param name="RequiereCambioDeContrasena">Si debe cambiar la contraseña.</param>
/// <param name="Idioma">Idioma preferido.</param>
public sealed record FilaDeUsuario(
    Guid Id,
    string IdentificadorExterno,
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    bool Activo,
    DateTimeOffset FechaAlta,
    string? HashContrasena,
    bool RequiereCambioDeContrasena,
    Idioma Idioma)
{
    /// <summary>
    /// Construye la entidad con sus permisos y empresas adicionales.
    /// </summary>
    /// <param name="permisos">Permisos personalizados.</param>
    /// <param name="empresasAdicionales">Empresas adicionales de un usuario de empresa cliente.</param>
    /// <returns>El usuario rehidratado.</returns>
    public Usuario Construir(IEnumerable<PermisoDeUsuario> permisos, IEnumerable<Guid> empresasAdicionales)
        => Usuario.Rehidratar(
            Id, IdentificadorExterno, NombreCompleto, Correo, Rol, EmpresaId, Activo, FechaAlta,
            HashContrasena, RequiereCambioDeContrasena, Idioma, permisos, empresasAdicionales);
}
