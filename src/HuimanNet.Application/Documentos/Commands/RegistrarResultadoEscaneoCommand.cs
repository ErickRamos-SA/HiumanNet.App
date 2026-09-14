namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Registra el veredicto del escaneo de malware sobre un blob cargado.
/// </summary>
/// <param name="RutaBlob">Ruta del blob dentro del contenedor de documentos.</param>
/// <param name="EsLimpio"><c>true</c> si el análisis no encontró amenazas.</param>
/// <param name="Motivo">Descripción del hallazgo cuando el archivo es malicioso.</param>
/// <remarks>
/// Lo origina Microsoft Defender for Storage, no una persona: la acción se
/// audita a nombre de <see cref="Common.IdentidadesDelSistema.Sistema"/>.
/// </remarks>
public sealed record RegistrarResultadoEscaneoCommand(
    string RutaBlob,
    bool EsLimpio,
    string? Motivo);
