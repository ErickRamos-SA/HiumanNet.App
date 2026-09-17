namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Renglón de una tabla por rangos: tarifa de ISR, subsidio al empleo o
/// cuota variable de cesantía y vejez.
/// </summary>
/// <param name="LimiteInferior">Límite inferior del rango, inclusive.</param>
/// <param name="LimiteSuperior">Límite superior del rango, inclusive, o <c>null</c> para "en adelante".</param>
/// <param name="CuotaFija">Cuota fija del rango (tarifas de ISR).</param>
/// <param name="Porcentaje">Porcentaje aplicable sobre el excedente, como fracción (16 % = 0.16).</param>
/// <param name="Valor">Valor directo del rango (subsidio al empleo).</param>
public sealed record RangoDeTabla(
    decimal LimiteInferior,
    decimal? LimiteSuperior,
    decimal CuotaFija,
    decimal Porcentaje,
    decimal Valor);
