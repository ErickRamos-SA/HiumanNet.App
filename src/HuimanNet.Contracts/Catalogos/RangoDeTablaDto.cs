namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Renglón de una tabla por rangos.
/// </summary>
/// <param name="LimiteInferior">Límite inferior.</param>
/// <param name="LimiteSuperior">Límite superior, o <c>null</c> para "en adelante".</param>
/// <param name="CuotaFija">Cuota fija.</param>
/// <param name="Porcentaje">Porcentaje como fracción.</param>
/// <param name="Valor">Valor directo.</param>
public sealed record RangoDeTablaDto(
    decimal LimiteInferior,
    decimal? LimiteSuperior,
    decimal CuotaFija,
    decimal Porcentaje,
    decimal Valor);
