using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Permiso personalizado de un usuario: habilita o deshabilita una acción con
/// independencia de lo que su rol concede por defecto.
/// </summary>
/// <param name="Accion">Acción afectada.</param>
/// <param name="Habilitado"><c>true</c> para conceder; <c>false</c> para retirar.</param>
public sealed record PermisoDeUsuario(AccionDelSistema Accion, bool Habilitado);
