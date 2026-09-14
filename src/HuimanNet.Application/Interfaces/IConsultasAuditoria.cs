using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Common;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Filtros de una consulta a la bitácora de auditoría.
/// </summary>
/// <param name="EmpresaId">
/// Empresa a la que se restringe la consulta, o <c>null</c> para todas.
/// Sólo el administrador puede consultar sin restricción de empresa.
/// </param>
/// <param name="Desde">Límite inferior del rango de fechas, en UTC.</param>
/// <param name="Hasta">Límite superior del rango de fechas, en UTC.</param>
/// <param name="Accion">Acción a filtrar, o <c>null</c> para todas.</param>
/// <param name="UsuarioId">Usuario a filtrar, o <c>null</c> para todos.</param>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="TamanoPagina">Número de asientos por página.</param>
public sealed record FiltroDeAuditoria(
    Guid? EmpresaId,
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    AccionAuditada? Accion,
    Guid? UsuarioId,
    int Pagina,
    int TamanoPagina);

/// <summary>
/// Lado de lectura de la bitácora de auditoría.
/// </summary>
public interface IConsultasAuditoria
{
    /// <summary>Tamaño de página empleado cuando el cliente no indica uno.</summary>
    const int TamanoPaginaPredeterminado = 25;

    /// <summary>Tamaño de página máximo admitido, para acotar el coste de la consulta.</summary>
    const int TamanoPaginaMaximo = 200;

    /// <summary>
    /// Consulta la bitácora aplicando filtros y paginación.
    /// </summary>
    /// <param name="filtro">Criterios de la consulta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La página de asientos, del más reciente al más antiguo.</returns>
    Task<PaginaDto<RegistroAuditoriaDto>> ConsultarAsync(
        FiltroDeAuditoria filtro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ajusta un tamaño de página solicitado a los límites admitidos.
    /// </summary>
    /// <param name="tamanoSolicitado">Tamaño pedido por el cliente.</param>
    /// <returns>Un valor entre 1 y <see cref="TamanoPaginaMaximo"/>.</returns>
    static int NormalizarTamanoPagina(int tamanoSolicitado)
        => tamanoSolicitado switch
        {
            <= 0 => TamanoPaginaPredeterminado,
            > TamanoPaginaMaximo => TamanoPaginaMaximo,
            _ => tamanoSolicitado,
        };
}
