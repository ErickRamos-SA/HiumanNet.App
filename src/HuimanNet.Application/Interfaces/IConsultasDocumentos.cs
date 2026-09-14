using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de los documentos: proyecta directamente a DTO sin
/// materializar entidades de dominio.
/// </summary>
/// <remarks>
/// Separar lecturas de escrituras (CQRS ligero) permite que las consultas
/// resuelvan en una sola sentencia SQL las uniones que la interfaz necesita
/// —por ejemplo el nombre de quien cargó el documento— sin contaminar el
/// modelo de dominio.
/// </remarks>
public interface IConsultasDocumentos
{
    /// <summary>
    /// Lista los documentos de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">
    /// Empresa del solicitante. Se aplica siempre en la cláusula <c>WHERE</c>:
    /// es la barrera de aislamiento entre empresas.
    /// </param>
    /// <param name="tipo">Tipo a filtrar, o <c>null</c> para devolver todos.</param>
    /// <param name="soloDescargables">
    /// Si es <c>true</c>, omite los documentos que aún no superaron el escaneo.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los documentos que satisfacen el filtro, del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<DocumentoDto>> ListarPorPeriodoAsync(
        Guid periodoId,
        Guid empresaId,
        TipoDocumento? tipo,
        bool soloDescargables,
        CancellationToken cancellationToken = default);
}
