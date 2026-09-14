namespace HuimanNet.Domain.Enums;

/// <summary>
/// Naturaleza de un concepto del catálogo de cálculo de nómina.
/// </summary>
/// <remarks>
/// El tipo determina cómo se agrupa el concepto en el recibo y en los reportes,
/// no cómo se calcula: el cálculo lo define siempre la fórmula del catálogo.
/// </remarks>
public enum TipoDeConcepto
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Valor intermedio (días trabajados, salario diario, bases gravables). No aparece en el recibo.</summary>
    Base = 1,

    /// <summary>Importe que suma al pago del trabajador.</summary>
    Percepcion = 2,

    /// <summary>Importe que se descuenta al trabajador.</summary>
    Deduccion = 3,

    /// <summary>Carga a cargo del patrón (cuotas IMSS patronales, INFONAVIT, ISN).</summary>
    Patronal = 4,

    /// <summary>Importe de facturación o costo al cliente (base facturable, comisión, IVA).</summary>
    Costo = 5,

    /// <summary>Total o resultado de referencia (bruto, neto, complemento sindical).</summary>
    Total = 6,
}
