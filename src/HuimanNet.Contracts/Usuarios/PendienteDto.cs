using System.Text.Json.Serialization;

namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Tarea sugerida en el panel de inicio.
/// </summary>
/// <param name="Tipo">Qué hay que hacer.</param>
/// <param name="Detalle">Detalle específico (por ejemplo, el nombre del período).</param>
/// <param name="EmpresaId">Empresa del período o de la corrida.</param>
/// <param name="PeriodoId">Período relacionado, si el pendiente es de un período.</param>
/// <param name="CorridaId">Corrida relacionada, si el pendiente es de una corrida.</param>
/// <remarks>
/// El servidor sólo describe el pendiente: cada cliente decide a qué pantalla
/// lleva, de modo que la web y la app navegan cada una a la suya.
/// </remarks>
public sealed record PendienteDto(TipoDePendiente Tipo, string Detalle, Guid EmpresaId, Guid? PeriodoId, Guid? CorridaId)
{
    /// <summary>Obtiene la clave del texto que describe el pendiente.</summary>
    /// <value>Por ejemplo <c>pendiente.subirDocumentos</c>; no viaja en el JSON.</value>
    [JsonIgnore]
    public string Clave => Tipo switch
    {
        TipoDePendiente.SubirDocumentos => "pendiente.subirDocumentos",
        TipoDePendiente.DescargarResultados => "pendiente.descargarResultados",
        TipoDePendiente.ProcesarPeriodo => "pendiente.procesarPeriodo",
        TipoDePendiente.CotejarCorrida => "pendiente.cotejarCorrida",
        _ => string.Empty,
    };
}
