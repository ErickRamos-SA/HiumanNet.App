using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Concepto calculado con su definición, para el detalle de un trabajador.
/// </summary>
/// <param name="Clave">Clave del concepto.</param>
/// <param name="Nombre">Nombre corto.</param>
/// <param name="Tipo">Naturaleza del concepto.</param>
/// <param name="Formula">Fórmula con la que se calculó.</param>
/// <param name="Importe">Importe calculado.</param>
/// <param name="VisibleEnRecibo">Si se muestra en el recibo.</param>
public sealed record ConceptoCalculadoDto(
    string Clave,
    string Nombre,
    TipoDeConcepto Tipo,
    string Formula,
    decimal Importe,
    bool VisibleEnRecibo);
