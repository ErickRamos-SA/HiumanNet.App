namespace HuimanNet.Domain.Enums;

/// <summary>
/// Base sobre la que se calcula la comisión al cliente.
/// </summary>
public enum ModalidadDeComision
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Porcentaje sobre el subtotal de costos (base, ISN, cargas sociales).</summary>
    SobreCosto = 1,

    /// <summary>Porcentaje sobre el bruto de incidencias.</summary>
    SobreBrutos = 2,
}
