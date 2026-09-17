namespace HuimanNet.Application.Catalogos;

/// <summary>Elimina un concepto.</summary>
/// <param name="ConceptoId">Concepto a eliminar.</param>
public sealed record EliminarConceptoCommand(Guid ConceptoId);
