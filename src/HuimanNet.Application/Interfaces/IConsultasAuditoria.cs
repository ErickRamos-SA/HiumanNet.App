using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Common;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de la bitácora de auditoría.
/// </summary>
public interface IConsultasAuditoria
{
    /// <summary>
    /// Consulta la bitácora aplicando filtros y paginación.
    /// </summary>
    /// <param name="filtro">Criterios de la consulta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La página de asientos, del más reciente al más antiguo.</returns>
    Task<PaginaDto<RegistroAuditoriaDto>> ConsultarAsync(
        FiltroDeAuditoria filtro, CancellationToken cancellationToken = default);
}
