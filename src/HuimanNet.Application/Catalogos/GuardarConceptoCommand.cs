using HuimanNet.Contracts.Catalogos;

namespace HuimanNet.Application.Catalogos;

/// <summary>Crea o actualiza un concepto.</summary>
/// <param name="ConceptoId">Concepto a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del concepto.</param>
public sealed record GuardarConceptoCommand(Guid? ConceptoId, GuardarConceptoRequest Datos);
