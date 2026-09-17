namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Aviso pendiente de envío, tal y como viaja por la cola.
/// </summary>
/// <param name="Tipo">Motivo del aviso.</param>
/// <param name="EmpresaId">Empresa a la que se refiere el aviso.</param>
/// <param name="PeriodoId">Período al que se refiere el aviso.</param>
/// <param name="DescripcionPeriodo">Descripción legible del período, para el cuerpo del correo.</param>
/// <param name="CantidadDocumentos">Número de documentos involucrados.</param>
/// <param name="Momento">Instante en que se generó el aviso, en UTC.</param>
/// <remarks>
/// El mensaje contiene únicamente identificadores y contadores: <b>nunca</b>
/// datos personales ni nombres de archivo, que podrían filtrarse en registros
/// de la cola (ESPECIFICACION.md §9, seguridad).
/// </remarks>
public sealed record AvisoPendiente(
    TipoDeAviso Tipo,
    Guid EmpresaId,
    Guid PeriodoId,
    string DescripcionPeriodo,
    int CantidadDocumentos,
    DateTimeOffset Momento);
