using HuimanNet.Contracts.Empresas;

namespace HuimanNet.Application.RazonesSociales;

/// <summary>
/// Crea o actualiza una razón social.
/// </summary>
/// <param name="RazonSocialId">Razón social a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos de la razón social.</param>
public sealed record GuardarRazonSocialCommand(Guid? RazonSocialId, GuardarRazonSocialRequest Datos);
