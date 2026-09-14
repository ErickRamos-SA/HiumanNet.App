namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Claves de los parámetros que el propio sistema necesita leer (no sólo las
/// fórmulas) para preparar las variables de cada trabajador.
/// </summary>
/// <remarks>
/// Los valores viven en el catálogo de parámetros, nunca aquí: estas constantes
/// sólo nombran las entradas que el sistema busca al resolver la zona, la tasa
/// de ISN y la prima de riesgo de un trabajador.
/// </remarks>
public static class ClavesDeParametro
{
    /// <summary>Salario mínimo diario de la zona A.</summary>
    public const string SalarioMinimoZonaA = "SM_ZONA_A";

    /// <summary>Salario mínimo diario de la zona B (frontera norte).</summary>
    public const string SalarioMinimoZonaB = "SM_ZONA_B";

    /// <summary>Tasa de ISN de la zona A, como fracción.</summary>
    public const string IsnZonaA = "ISN_ZONA_A";

    /// <summary>Tasa de ISN de la zona B, como fracción.</summary>
    public const string IsnZonaB = "ISN_ZONA_B";

    /// <summary>Prima de riesgo de trabajo por defecto cuando la razón social no define una.</summary>
    public const string PrimaRiesgoPredeterminada = "IMSS_PRT_DEFAULT";

    /// <summary>Días del período de pago cuando no hay incidencia capturada.</summary>
    public const string DiasPeriodoPredeterminados = "DIAS_PERIODO_NOMINA";

    /// <summary>
    /// Obtiene las claves que el sistema exige que existan en el catálogo.
    /// </summary>
    /// <value>Lista de sólo lectura.</value>
    public static IReadOnlyList<string> Obligatorias { get; } =
    [
        SalarioMinimoZonaA, SalarioMinimoZonaB, IsnZonaA, IsnZonaB, PrimaRiesgoPredeterminada, DiasPeriodoPredeterminados,
    ];
}
