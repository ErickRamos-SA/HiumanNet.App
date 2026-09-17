namespace HuimanNet.Domain.Enums;

/// <summary>
/// Estado de una corrida de cálculo de nómina.
/// </summary>
/// <remarks>
/// En esta primera etapa el sistema calcula en paralelo con el equipo de nómina:
/// una corrida se coteja contra el resultado manual y sólo se aprueba cuando
/// las diferencias están dentro de la tolerancia acordada.
/// </remarks>
public enum EstadoDeCorrida
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Calculada por el sistema; pendiente de cotejo.</summary>
    Calculada = 1,

    /// <summary>Cotejada contra el resultado manual.</summary>
    Cotejada = 2,

    /// <summary>Aprobada como resultado definitivo del período.</summary>
    Aprobada = 3,

    /// <summary>Descartada; se conserva sólo como historial.</summary>
    Descartada = 4,
}
