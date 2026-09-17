using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Api.Seguridad;

/// <summary>
/// Implementación de <see cref="IUsuarioActual"/> con ámbito de petición HTTP.
/// </summary>
/// <remarks>
/// La rellena <see cref="MiddlewareDeUsuarioActual"/> después de la
/// autenticación, con el usuario leído de la base de datos: el rol, la empresa
/// y los permisos que ven los casos de uso son siempre los vigentes, no los que
/// traía el token al emitirse.
/// </remarks>
public sealed class UsuarioActualDeHttpContext : IUsuarioActual
{
    private Usuario? _usuario;

    /// <inheritdoc/>
    public Guid UsuarioId => Requerido().Id;

    /// <inheritdoc/>
    public string NombreCompleto => Requerido().NombreCompleto;

    /// <inheritdoc/>
    public string Correo => Requerido().Correo;

    /// <inheritdoc/>
    public RolUsuario Rol => Requerido().Rol;

    /// <inheritdoc/>
    public Guid? EmpresaId => Requerido().EmpresaId;

    /// <inheritdoc/>
    public IReadOnlyList<Guid> Empresas => Requerido().Empresas;

    /// <inheritdoc/>
    public IReadOnlyList<PermisoDeUsuario> Permisos => Requerido().Permisos;

    /// <inheritdoc/>
    public Idioma Idioma => Requerido().Idioma;

    /// <inheritdoc/>
    public bool RequiereCambioDeContrasena => Requerido().RequiereCambioDeContrasena;

    /// <inheritdoc/>
    public string? DireccionIp { get; private set; }

    /// <inheritdoc/>
    public bool EstaAutenticado => _usuario is not null;

    /// <summary>
    /// Establece el usuario resuelto para la petición.
    /// </summary>
    /// <param name="usuario">Usuario local resuelto a partir del token.</param>
    /// <param name="direccionIp">Dirección IP de origen, para la bitácora.</param>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="usuario"/> es <c>null</c>.</exception>
    public void Establecer(Usuario usuario, string? direccionIp)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        _usuario = usuario;
        DireccionIp = direccionIp;
    }

    /// <summary>Obtiene el usuario resuelto para la petición.</summary>
    /// <returns>El usuario.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si no se resolvió ninguno, lo que indica un endpoint sin <c>RequireAuthorization</c>.
    /// </exception>
    private Usuario Requerido()
        => _usuario ?? throw new InvalidOperationException(
            "No hay usuario resuelto en la petición actual. ¿Falta RequireAuthorization en el endpoint?");
}
