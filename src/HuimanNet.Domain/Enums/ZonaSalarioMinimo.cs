namespace HuimanNet.Domain.Enums;

/// <summary>
/// Zona geográfica de salario mínimo.
/// </summary>
public enum ZonaSalarioMinimo
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Zona A: resto del país.</summary>
    A = 1,

    /// <summary>Zona B: Zona Libre de la Frontera Norte.</summary>
    B = 2,
}
