namespace HuimanNet.Application.Empresas.Queries;

/// <summary>
/// Consulta el catálogo de empresas cliente.
/// </summary>
/// <param name="SoloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
public sealed record ListarEmpresasQuery(bool SoloActivas);
