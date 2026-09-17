using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Documentos.Queries;

/// <summary>
/// Consulta los documentos de un período.
/// </summary>
/// <param name="PeriodoId">Período consultado.</param>
/// <param name="Tipo">Tipo a filtrar, o <c>null</c> para devolver todos.</param>
/// <param name="SoloDescargables">
/// Si es <c>true</c>, omite los documentos que aún no superaron el escaneo.
/// </param>
/// <param name="EmpresaId">
/// Empresa objetivo. Sólo la aportan los roles transversales; para la empresa
/// cliente se impone la del token.
/// </param>
public sealed record ListarDocumentosPorPeriodoQuery(
    Guid PeriodoId,
    TipoDocumento? Tipo,
    bool SoloDescargables,
    Guid? EmpresaId);
