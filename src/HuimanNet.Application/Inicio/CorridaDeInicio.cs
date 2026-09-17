namespace HuimanNet.Application.Inicio;

/// <summary>
/// Corrida calculada pendiente de cotejo, para el panel de inicio.
/// </summary>
/// <param name="CorridaId">Corrida.</param>
/// <param name="EmpresaId">Empresa de la corrida.</param>
/// <param name="Numero">Número consecutivo dentro del período.</param>
/// <param name="DescripcionPeriodo">Descripción del período.</param>
public sealed record CorridaDeInicio(Guid CorridaId, Guid EmpresaId, int Numero, string DescripcionPeriodo);
