namespace HuimanNet.Domain.Enums;

/// <summary>
/// Regla con la que se determina la tasa del Impuesto Sobre Nóminas de una razón social.
/// </summary>
public enum ZonaIsn
{
    /// <summary>La tasa se toma de la zona de salario mínimo de cada trabajador.</summary>
    SegunZonaDelTrabajador = 0,

    /// <summary>Se fuerza la tasa de la zona A para todos los trabajadores.</summary>
    ForzarZonaA = 1,

    /// <summary>Se fuerza la tasa de la zona B para todos los trabajadores.</summary>
    ForzarZonaB = 2,
}
