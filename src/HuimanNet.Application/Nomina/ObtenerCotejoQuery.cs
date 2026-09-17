namespace HuimanNet.Application.Nomina;

/// <summary>
/// Obtiene un cotejo con su detalle.
/// </summary>
/// <param name="CotejoId">Cotejo consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerCotejoQuery(Guid CotejoId, Guid? EmpresaId);
