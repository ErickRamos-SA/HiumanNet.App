namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Cotejo de una corrida.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="CorridaId">Corrida.</param>
/// <param name="FechaCotejo">Instante del cotejo, en UTC.</param>
/// <param name="Usuario">Nombre del usuario que cotejó.</param>
/// <param name="NombreArchivo">Archivo manual.</param>
/// <param name="ToleranciaAbsoluta">Tolerancia aplicada.</param>
/// <param name="TotalComparaciones">Comparaciones realizadas.</param>
/// <param name="TotalFueraDeTolerancia">Comparaciones fuera de tolerancia.</param>
/// <param name="Diferencias">Detalle; vacío en los listados.</param>
public sealed record CotejoDto(
    Guid Id,
    Guid CorridaId,
    DateTimeOffset FechaCotejo,
    string Usuario,
    string NombreArchivo,
    decimal ToleranciaAbsoluta,
    int TotalComparaciones,
    int TotalFueraDeTolerancia,
    IReadOnlyList<DiferenciaDeCotejoDto> Diferencias);
