using HuimanNet.Contracts.Catalogos;

namespace HuimanNet.Application.Catalogos;

/// <summary>Crea o actualiza un parámetro.</summary>
/// <param name="ParametroId">Parámetro a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del parámetro.</param>
public sealed record GuardarParametroCommand(Guid? ParametroId, GuardarParametroRequest Datos);
