namespace HuimanNet.Contracts.Common;

/// <summary>
/// Configuración pública del servidor que los clientes consultan antes de autenticarse.
/// </summary>
/// <param name="ModoDeIdentidad"><c>Entra</c> o <c>Local</c>.</param>
/// <param name="Version">Versión de la API.</param>
/// <param name="ProveedorDeAlmacenamiento"><c>Local</c> o <c>AzureBlob</c>; informativo.</param>
public sealed record ConfiguracionPublicaDto(string ModoDeIdentidad, string Version, string ProveedorDeAlmacenamiento);
