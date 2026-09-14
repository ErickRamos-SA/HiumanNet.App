using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Common;

/// <summary>
/// Escribe asientos de auditoría firmados con la identidad de la petición en curso.
/// </summary>
/// <remarks>
/// Evita que cada caso de uso repita la construcción del asiento. El detalle
/// nunca debe contener datos personales ni identificadores fiscales.
/// </remarks>
public sealed class RegistradorDeAuditoria
{
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUsuarioActual _usuario;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RegistradorDeAuditoria"/>.
    /// </summary>
    /// <param name="auditoria">Repositorio de la bitácora.</param>
    /// <param name="usuario">Identidad efectiva del solicitante.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    public RegistradorDeAuditoria(IAuditoriaRepository auditoria, IUsuarioActual usuario, TimeProvider reloj)
    {
        _auditoria = auditoria;
        _usuario = usuario;
        _reloj = reloj;
    }

    /// <summary>
    /// Registra una acción completada con éxito.
    /// </summary>
    /// <param name="accion">Acción ejecutada.</param>
    /// <param name="empresaId">Empresa en cuyo ámbito ocurrió, si aplica.</param>
    /// <param name="recursoTipo">Tipo de recurso afectado.</param>
    /// <param name="recursoId">Identificador del recurso, si aplica.</param>
    /// <param name="detalle">Información técnica sin datos personales.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public Task ExitoAsync(
        AccionAuditada accion,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        string? detalle,
        CancellationToken cancellationToken = default)
        => _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                accion, _usuario.UsuarioId, empresaId, recursoTipo, recursoId,
                _reloj.GetUtcNow(), detalle, _usuario.DireccionIp),
            cancellationToken);

    /// <summary>
    /// Registra un intento rechazado.
    /// </summary>
    /// <param name="accion">Acción intentada.</param>
    /// <param name="empresaId">Empresa en cuyo ámbito se intentó, si aplica.</param>
    /// <param name="recursoTipo">Tipo de recurso afectado.</param>
    /// <param name="recursoId">Identificador del recurso, si aplica.</param>
    /// <param name="motivo">Motivo técnico del rechazo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public Task FalloAsync(
        AccionAuditada accion,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        string motivo,
        CancellationToken cancellationToken = default)
        => _auditoria.AgregarAsync(
            RegistroAuditoria.Fallido(
                accion, _usuario.UsuarioId, empresaId, recursoTipo, recursoId,
                _reloj.GetUtcNow(), motivo, _usuario.DireccionIp),
            cancellationToken);
}
