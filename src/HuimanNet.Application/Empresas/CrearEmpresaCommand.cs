namespace HuimanNet.Application.Empresas;

/// <summary>
/// Crea una empresa cliente.
/// </summary>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">Identificador fiscal.</param>
public sealed record CrearEmpresaCommand(string RazonSocial, string IdentificadorFiscal);
