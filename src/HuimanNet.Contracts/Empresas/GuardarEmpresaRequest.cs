namespace HuimanNet.Contracts.Empresas;

/// <summary>
/// Petición para crear o actualizar una empresa cliente.
/// </summary>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">RFC o identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
public sealed record GuardarEmpresaRequest(string RazonSocial, string IdentificadorFiscal, bool Activa = true);
