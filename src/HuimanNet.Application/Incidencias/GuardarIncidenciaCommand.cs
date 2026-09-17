using HuimanNet.Contracts.Incidencias;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Captura o actualiza la incidencia de un contrato en un período.
/// </summary>
/// <param name="Datos">Datos de la incidencia.</param>
public sealed record GuardarIncidenciaCommand(GuardarIncidenciaRequest Datos);
