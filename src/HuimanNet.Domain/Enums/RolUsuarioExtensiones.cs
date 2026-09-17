namespace HuimanNet.Domain.Enums;

/// <summary>
/// Reglas que dependen sólo del rol de un usuario.
/// </summary>
/// <remarks>
/// Es el punto único de estas reglas: la política de acceso, los anfitriones y
/// los clientes (web y app) las consultan aquí en lugar de repetir la lista de
/// roles.
/// </remarks>
public static class RolUsuarioExtensiones
{
    /// <summary>
    /// Indica si el rol opera transversalmente sobre todas las empresas.
    /// </summary>
    /// <param name="rol">Rol consultado.</param>
    /// <returns><c>true</c> para el operador de nómina y el administrador.</returns>
    public static bool EsTransversal(this RolUsuario rol)
        => rol is RolUsuario.OperadorNomina or RolUsuario.Administrador;
}
