namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Corrida con sus resultados y la facturación estimada por razón social.
/// </summary>
/// <param name="Corrida">Corrida.</param>
/// <param name="Resultados">Resultados por contrato.</param>
/// <param name="Facturacion">Facturación por razón social.</param>
public sealed record ResumenDeCorridaDto(
    CorridaDeNominaDto Corrida,
    IReadOnlyList<ResultadoDeNominaDto> Resultados,
    IReadOnlyList<FacturacionDeCorridaDto> Facturacion);
