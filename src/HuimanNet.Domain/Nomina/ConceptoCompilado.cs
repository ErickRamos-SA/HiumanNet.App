using HuimanNet.Domain.Formulas;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Concepto con su fórmula ya compilada, listo para evaluarse.
/// </summary>
/// <param name="Concepto">Definición del catálogo.</param>
/// <param name="Formula">Fórmula compilada.</param>
public sealed record ConceptoCompilado(ConceptoDeNomina Concepto, FormulaCompilada Formula);
