using HuimanNet.Contracts.Usuarios;

namespace HuimanNet.Application.Usuarios;

/// <summary>Crea o actualiza un usuario.</summary>
/// <param name="UsuarioId">Usuario a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del usuario.</param>
public sealed record GuardarUsuarioCommand(Guid? UsuarioId, GuardarUsuarioRequest Datos);
