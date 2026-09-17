using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Permiso personalizado de un usuario.
/// </summary>
/// <param name="Accion">Acción.</param>
/// <param name="Habilitado">Si se concede o se retira.</param>
public sealed record PermisoDto(AccionDelSistema Accion, bool Habilitado);
