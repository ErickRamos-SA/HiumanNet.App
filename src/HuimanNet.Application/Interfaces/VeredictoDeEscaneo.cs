namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Veredicto de un análisis antimalware.
/// </summary>
/// <param name="EsLimpio"><c>true</c> si no se detectaron amenazas.</param>
/// <param name="Motivo">Descripción del hallazgo cuando el archivo es malicioso.</param>
public sealed record VeredictoDeEscaneo(bool EsLimpio, string? Motivo);
