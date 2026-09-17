namespace HuimanNet.Application.Nomina;

/// <summary>
/// Coteja una corrida contra el archivo de resultados del equipo de nómina.
/// </summary>
/// <param name="CorridaId">Corrida a cotejar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DocumentoId">Archivo de resultados o de ajuste del mismo período, ya disponible.</param>
/// <param name="ToleranciaAbsoluta">Diferencia absoluta aceptada por concepto.</param>
public sealed record CotejarNominaCommand(Guid CorridaId, Guid? EmpresaId, Guid DocumentoId, decimal ToleranciaAbsoluta);
