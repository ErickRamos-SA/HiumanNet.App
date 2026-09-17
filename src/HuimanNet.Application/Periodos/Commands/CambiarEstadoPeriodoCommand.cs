using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Periodos.Commands;

/// <summary>
/// Avanza un período al siguiente estado del ciclo de intercambio.
/// </summary>
/// <param name="PeriodoId">Período afectado.</param>
/// <param name="NuevoEstado">Estado destino.</param>
/// <param name="Comentario">Nota opcional que se guarda en la bitácora.</param>
/// <param name="EmpresaId">Empresa propietaria del período.</param>
public sealed record CambiarEstadoPeriodoCommand(
    Guid PeriodoId,
    EstadoPeriodo NuevoEstado,
    string? Comentario,
    Guid? EmpresaId);
