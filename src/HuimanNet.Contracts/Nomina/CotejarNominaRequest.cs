namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Petición para cotejar una corrida contra el archivo de resultados manual.
/// </summary>
/// <param name="CorridaId">Corrida a cotejar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DocumentoId">Archivo de resultados o de ajuste publicado en el mismo período.</param>
/// <param name="ToleranciaAbsoluta">Diferencia absoluta aceptada por concepto.</param>
public sealed record CotejarNominaRequest(
    Guid CorridaId,
    Guid? EmpresaId,
    Guid DocumentoId,
    decimal ToleranciaAbsoluta);
