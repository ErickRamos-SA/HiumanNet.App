namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>Rango de una tabla del catálogo inicial.</summary>
/// <param name="LimiteInferior">Límite inferior.</param>
/// <param name="LimiteSuperior">Límite superior, o <c>null</c>.</param>
/// <param name="CuotaFija">Cuota fija.</param>
/// <param name="Porcentaje">Porcentaje como fracción.</param>
/// <param name="Valor">Valor directo.</param>
public sealed record RangoInicial(decimal LimiteInferior, decimal? LimiteSuperior, decimal CuotaFija, decimal Porcentaje, decimal Valor);
