namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Valor calculado de un concepto.
/// </summary>
/// <param name="Clave">Clave del concepto.</param>
/// <param name="Importe">Importe calculado, sin redondeo adicional al de la fórmula.</param>
public sealed record ValorDeConcepto(string Clave, decimal Importe);
