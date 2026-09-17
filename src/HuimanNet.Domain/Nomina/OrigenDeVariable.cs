namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Origen de una variable de cálculo.
/// </summary>
public enum OrigenDeVariable
{
    /// <summary>Se toma del contrato del trabajador.</summary>
    Contrato = 1,

    /// <summary>Se toma de las incidencias capturadas para el período.</summary>
    Incidencia = 2,

    /// <summary>Se toma de la configuración de la razón social.</summary>
    RazonSocial = 3,

    /// <summary>La resuelve el sistema combinando contrato, razón social y parámetros.</summary>
    Derivada = 4,
}
