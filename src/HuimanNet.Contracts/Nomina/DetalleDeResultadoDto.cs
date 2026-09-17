namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Detalle completo del resultado de un trabajador.
/// </summary>
/// <param name="Resultado">Resumen del resultado.</param>
/// <param name="Conceptos">Conceptos en orden de evaluación.</param>
/// <param name="Variables">Variables de entrada con las que se calculó.</param>
public sealed record DetalleDeResultadoDto(
    ResultadoDeNominaDto Resultado,
    IReadOnlyList<ConceptoCalculadoDto> Conceptos,
    IReadOnlyList<ValorDto> Variables);
