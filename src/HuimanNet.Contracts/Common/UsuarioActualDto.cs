using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Common;

/// <summary>
/// Identidad efectiva del usuario que emite la petición, tal y como la resolvió
/// el servidor a partir del token o de la sesión local.
/// </summary>
/// <param name="UsuarioId">Identificador local del usuario.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="Correo">Dirección de correo de notificación.</param>
/// <param name="Rol">Rol funcional resuelto por el servidor.</param>
/// <param name="EmpresaId">Empresa principal, si su rol no es transversal.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa principal, si aplica.</param>
/// <param name="Idioma">Idioma preferido de la interfaz.</param>
/// <param name="Acciones">Acciones habilitadas para el usuario, combinando su rol y sus permisos personalizados.</param>
/// <param name="RequiereCambioDeContrasena">Si debe cambiar su contraseña local antes de operar.</param>
/// <param name="Empresas">
/// Empresas en las que opera un usuario de empresa cliente, la principal
/// primero; vacía para los roles transversales, que operan sobre todas.
/// </param>
/// <remarks>
/// El cliente la usa para adaptar la interfaz. <b>Nunca</b> es fuente de
/// autorización: cada petición se vuelve a autorizar en el servidor.
/// </remarks>
public sealed record UsuarioActualDto(
    Guid UsuarioId,
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    string? EmpresaRazonSocial,
    Idioma Idioma,
    IReadOnlyList<AccionDelSistema> Acciones,
    bool RequiereCambioDeContrasena,
    IReadOnlyList<EmpresaDto> Empresas)
{
    /// <summary>
    /// Indica si el usuario elige la empresa de trabajo.
    /// </summary>
    /// <value><c>true</c> para los roles transversales y para la empresa cliente con más de una empresa.</value>
    public bool EligeEmpresa => EsTransversal || Empresas.Count > 1;

    /// <summary>
    /// Indica si el usuario tiene habilitada una acción.
    /// </summary>
    /// <param name="accion">Acción consultada.</param>
    /// <returns><c>true</c> si la acción está en la lista de habilitadas.</returns>
    public bool Puede(AccionDelSistema accion) => Acciones.Contains(accion);

    /// <summary>
    /// Indica si el rol opera sobre todas las empresas.
    /// </summary>
    /// <value><c>true</c> para operador de nómina y administrador.</value>
    public bool EsTransversal => Rol.EsTransversal();
}
