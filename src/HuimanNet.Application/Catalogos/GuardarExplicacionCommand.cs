using HuimanNet.Contracts.Catalogos;

namespace HuimanNet.Application.Catalogos;

/// <summary>Crea o actualiza una sección de explicación.</summary>
/// <param name="ExplicacionId">Sección a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos de la sección.</param>
public sealed record GuardarExplicacionCommand(Guid? ExplicacionId, GuardarExplicacionRequest Datos);
