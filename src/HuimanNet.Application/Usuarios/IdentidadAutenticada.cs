using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Identidad autenticada por el proveedor, ya leída de su token o cookie, que
/// se traduce a un usuario del portal.
/// </summary>
/// <param name="IdentificadorExterno">Identificador del sujeto en el proveedor.</param>
/// <param name="EsCuentaExterna">
/// <c>true</c> si la autentica un proveedor externo (Microsoft Entra), que
/// permite enlazar usuarios dados de alta por correo y aprovisionar los que
/// traen un rol de aplicación; <c>false</c> en el modo de cuentas locales.
/// </param>
/// <param name="Correo">Correo indicado por el proveedor, si lo hay.</param>
/// <param name="NombreCompleto">Nombre indicado por el proveedor, si lo hay.</param>
/// <param name="RolDeAplicacion">Rol del portal que asigna el proveedor, si lo hay.</param>
/// <param name="EmpresaIndicada">Valor del claim de empresa, sin interpretar, si lo hay.</param>
public sealed record IdentidadAutenticada(
    string IdentificadorExterno,
    bool EsCuentaExterna,
    string? Correo,
    string? NombreCompleto,
    RolUsuario? RolDeAplicacion,
    string? EmpresaIndicada);
