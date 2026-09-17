namespace HuimanNet.Application.RazonesSociales;

/// <summary>
/// Lista las razones sociales de una empresa.
/// </summary>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="SoloActivas">Si es <c>true</c>, omite las dadas de baja.</param>
public sealed record ListarRazonesSocialesQuery(Guid? EmpresaId, bool SoloActivas);
