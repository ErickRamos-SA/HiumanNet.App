using HuimanNet.Contracts.Catalogos;

namespace HuimanNet.Application.Catalogos;

/// <summary>Crea o actualiza una tabla por rangos.</summary>
/// <param name="TablaId">Tabla a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos de la tabla.</param>
public sealed record GuardarTablaCommand(Guid? TablaId, GuardarTablaRequest Datos);
