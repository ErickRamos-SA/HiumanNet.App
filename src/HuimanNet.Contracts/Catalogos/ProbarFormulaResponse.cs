namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Resultado de probar una fórmula.
/// </summary>
/// <param name="Valida">Si la fórmula compiló y se evaluó.</param>
/// <param name="Resultado">Valor obtenido, si es válida.</param>
/// <param name="Error">Mensaje de error, si no es válida.</param>
/// <param name="Referencias">Identificadores que la fórmula referencia.</param>
/// <param name="ConceptosPrevios">Valores de los conceptos del catálogo evaluados con los mismos valores de ejemplo.</param>
public sealed record ProbarFormulaResponse(
    bool Valida,
    decimal? Resultado,
    string? Error,
    IReadOnlyList<string> Referencias,
    IReadOnlyList<Contracts.Nomina.ValorDto> ConceptosPrevios);
