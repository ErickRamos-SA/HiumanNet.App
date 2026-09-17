namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Par clave-valor numérico.
/// </summary>
/// <param name="Clave">Clave.</param>
/// <param name="Valor">Valor.</param>
public sealed record ValorDto(string Clave, decimal Valor);
