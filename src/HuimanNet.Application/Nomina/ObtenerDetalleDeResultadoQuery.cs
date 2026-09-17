namespace HuimanNet.Application.Nomina;

/// <summary>
/// Obtiene el detalle de conceptos de un resultado.
/// </summary>
/// <param name="ResultadoId">Resultado consultado.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
public sealed record ObtenerDetalleDeResultadoQuery(Guid ResultadoId, Guid? EmpresaId);
