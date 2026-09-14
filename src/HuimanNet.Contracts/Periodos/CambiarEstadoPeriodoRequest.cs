using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Periodos;

/// <summary>
/// Petición para avanzar un período al siguiente estado del ciclo.
/// </summary>
/// <param name="NuevoEstado">Estado destino. El dominio rechaza los retrocesos.</param>
/// <param name="Comentario">Nota opcional que se guarda en la bitácora de auditoría.</param>
public sealed record CambiarEstadoPeriodoRequest(
    EstadoPeriodo NuevoEstado,
    string? Comentario = null);
