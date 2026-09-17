namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Transacción en curso sobre el almacén de datos.
/// </summary>
/// <remarks>
/// Si se libera sin haber confirmado, la implementación debe revertir: así una
/// excepción a mitad de un caso de uso nunca deja escrituras parciales.
/// </remarks>
public interface ITransaccion : IAsyncDisposable
{
    /// <summary>
    /// Confirma todas las escrituras realizadas dentro de la transacción.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revierte todas las escrituras realizadas dentro de la transacción.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task RevertirAsync(CancellationToken cancellationToken = default);
}
