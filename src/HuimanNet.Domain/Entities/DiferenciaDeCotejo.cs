namespace HuimanNet.Domain.Entities;

/// <summary>
/// Diferencia detectada entre el cálculo del sistema y el resultado manual
/// para un concepto de un trabajador.
/// </summary>
/// <param name="ClaveEmpleado">Clave del trabajador en el archivo manual.</param>
/// <param name="ContratoId">Contrato del sistema con el que se emparejó, o <c>null</c> si no se encontró.</param>
/// <param name="ConceptoClave">Concepto comparado.</param>
/// <param name="ImporteSistema">Importe calculado por el sistema, o <c>null</c> si el sistema no lo calculó.</param>
/// <param name="ImporteManual">Importe del archivo manual, o <c>null</c> si no venía.</param>
/// <param name="Diferencia">Sistema menos manual.</param>
/// <param name="DentroDeTolerancia">Si la diferencia absoluta no supera la tolerancia del cotejo.</param>
public sealed record DiferenciaDeCotejo(
    string ClaveEmpleado,
    Guid? ContratoId,
    string ConceptoClave,
    decimal? ImporteSistema,
    decimal? ImporteManual,
    decimal Diferencia,
    bool DentroDeTolerancia);
