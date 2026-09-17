namespace HuimanNet.Domain.Enums;

/// <summary>
/// Clasificación del movimiento de un trabajador dentro de un período.
/// </summary>
public enum TipoDeMovimiento
{
    /// <summary>Nómina ordinaria del período.</summary>
    Ordinaria = 1,

    /// <summary>Última nómina del trabajador: incluye finiquito y se factura por separado.</summary>
    Finiquito = 2,
}
