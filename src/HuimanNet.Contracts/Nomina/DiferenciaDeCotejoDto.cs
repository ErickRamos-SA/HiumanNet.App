namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Diferencia entre el sistema y el resultado manual.
/// </summary>
/// <param name="ClaveEmpleado">Clave del trabajador.</param>
/// <param name="ConceptoClave">Concepto comparado.</param>
/// <param name="ImporteSistema">Importe del sistema, o <c>null</c>.</param>
/// <param name="ImporteManual">Importe manual, o <c>null</c>.</param>
/// <param name="Diferencia">Sistema menos manual.</param>
/// <param name="DentroDeTolerancia">Si la diferencia está dentro de la tolerancia.</param>
public sealed record DiferenciaDeCotejoDto(
    string ClaveEmpleado,
    string ConceptoClave,
    decimal? ImporteSistema,
    decimal? ImporteManual,
    decimal Diferencia,
    bool DentroDeTolerancia);
