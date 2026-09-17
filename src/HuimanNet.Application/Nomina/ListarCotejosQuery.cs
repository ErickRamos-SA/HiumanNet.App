namespace HuimanNet.Application.Nomina;

/// <summary>
/// Lista los cotejos de una corrida.
/// </summary>
/// <param name="CorridaId">Corrida consultada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ListarCotejosQuery(Guid CorridaId, Guid? EmpresaId);
