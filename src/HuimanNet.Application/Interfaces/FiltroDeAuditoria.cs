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
