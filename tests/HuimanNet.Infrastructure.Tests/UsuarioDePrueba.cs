using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>Identidad fija de un usuario sembrado o dado de alta, sin contraseña ni token.</summary>
internal sealed class UsuarioDePrueba : IUsuarioActual
{
    private Usuario? _usuario;

    public Guid UsuarioId => Requerido().Id;

    public string NombreCompleto => Requerido().NombreCompleto;

    public string Correo => Requerido().Correo;

    public RolUsuario Rol => Requerido().Rol;

    public Guid? EmpresaId => Requerido().EmpresaId;

    public IReadOnlyList<Guid> Empresas => Requerido().Empresas;

    public IReadOnlyList<PermisoDeUsuario> Permisos => Requerido().Permisos;

    public Idioma Idioma => Requerido().Idioma;

    public bool RequiereCambioDeContrasena => false;

    public string? DireccionIp => "127.0.0.1";

    public bool EstaAutenticado => _usuario is not null;

    public void Establecer(Usuario usuario) => _usuario = usuario;

    private Usuario Requerido() => _usuario ?? throw new InvalidOperationException("Usuario de prueba sin establecer.");
}
