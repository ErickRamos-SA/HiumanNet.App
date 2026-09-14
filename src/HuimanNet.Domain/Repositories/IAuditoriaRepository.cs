using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Escritura de la bitácora de auditoría. Los asientos son inmutables: este
/// contrato sólo admite inserción.
/// </summary>
/// <remarks>
/// La lectura de la bitácora vive en el lado de consultas
/// (<c>IConsultasAuditoria</c>, capa de aplicación), conforme al CQRS ligero
/// adoptado en la solución (ESPECIFICACION.md §9).
/// </remarks>
public interface IAuditoriaRepository
{
    /// <summary>
    /// Inserta un asiento en la bitácora.
    /// </summary>
    /// <param name="registro">Asiento a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(RegistroAuditoria registro, CancellationToken cancellationToken = default);
}
