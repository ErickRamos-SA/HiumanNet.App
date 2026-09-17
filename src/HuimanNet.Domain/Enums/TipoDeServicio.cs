namespace HuimanNet.Domain.Enums;

/// <summary>
/// Esquema de servicio pactado con la razón social: determina la base
/// facturable y si existe un complemento pagado vía sindicato.
/// </summary>
public enum TipoDeServicio
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>La nómina completa se paga y factura como nómina fiscal.</summary>
    Nomina = 1,

    /// <summary>La parte fiscal se paga con el salario registrado y la diferencia se entrega vía sindicato o cooperativa.</summary>
    Maquila = 2,
}
