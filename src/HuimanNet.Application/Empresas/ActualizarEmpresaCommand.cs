namespace HuimanNet.Application.Empresas;

/// <summary>
/// Actualiza una empresa cliente.
/// </summary>
/// <param name="EmpresaId">Empresa a actualizar.</param>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">Identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
public sealed record ActualizarEmpresaCommand(Guid EmpresaId, string RazonSocial, string IdentificadorFiscal, bool Activa);
