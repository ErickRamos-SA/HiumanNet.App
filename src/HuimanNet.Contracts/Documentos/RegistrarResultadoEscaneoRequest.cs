namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Veredicto del escaneo de malware sobre un blob recién cargado.
/// </summary>
/// <param name="RutaBlob">Ruta del blob dentro del contenedor de documentos.</param>
/// <param name="EsLimpio"><c>true</c> si el análisis no encontró amenazas.</param>
/// <param name="Motivo">Descripción del hallazgo cuando el archivo es malicioso.</param>
/// <remarks>
/// Lo publica Microsoft Defender for Storage a través de Event Grid. El endpoint
/// que lo recibe exige autenticación de servicio: nunca debe quedar expuesto de
/// forma anónima.
/// </remarks>
public sealed record RegistrarResultadoEscaneoRequest(
    string RutaBlob,
    bool EsLimpio,
    string? Motivo = null);
