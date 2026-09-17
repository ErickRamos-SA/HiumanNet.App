namespace HuimanNet.Application.Empresas;

/// <summary>
/// Lista las empresas con sus contadores administrativos.
/// </summary>
/// <param name="SoloActivas">Si es <c>true</c>, omite las dadas de baja.</param>
public sealed record ListarEmpresasDetalleQuery(bool SoloActivas);
