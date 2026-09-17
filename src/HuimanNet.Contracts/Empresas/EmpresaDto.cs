namespace HuimanNet.Contracts.Empresas;

/// <summary>
/// Proyección de lectura de una empresa cliente.
/// </summary>
/// <param name="Id">Identificador único de la empresa.</param>
/// <param name="RazonSocial">Razón social mostrada en la interfaz.</param>
/// <param name="Activa">Indica si la empresa puede operar en el portal.</param>
/// <remarks>
/// No incluye el identificador fiscal: es un dato sensible y ningún cliente lo
/// necesita para operar el portal de documentos.
/// </remarks>
public sealed record EmpresaDto(Guid Id, string RazonSocial, bool Activa);
