namespace HuimanNet.Domain.Enums;

/// <summary>
/// Modalidad del aviso de retención de un crédito INFONAVIT.
/// </summary>
public enum TipoDeCreditoInfonavit
{
    /// <summary>El trabajador no tiene crédito vigente.</summary>
    Ninguno = 0,

    /// <summary>Importe fijo mensual.</summary>
    CuotaFija = 1,

    /// <summary>Factor expresado en veces salario mínimo, convertido con la UMI.</summary>
    VecesSalarioMinimo = 2,

    /// <summary>Porcentaje sobre el salario diario.</summary>
    Porcentaje = 3,
}
