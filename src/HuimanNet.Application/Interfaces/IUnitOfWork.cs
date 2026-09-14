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

/// <summary>
/// Coordina varias escrituras de un mismo caso de uso en una única transacción.
/// </summary>
/// <remarks>
/// Los repositorios comparten la conexión y la transacción abiertas aquí; con
/// ADO.NET puro, ésta es la pieza que sustituye al <c>DbContext</c> de un ORM
/// (ESPECIFICACION.md §6.1).
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Abre una transacción sobre la conexión de la petición actual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La transacción abierta, que debe confirmarse o liberarse.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si ya hay una transacción abierta en el ámbito actual.
    /// </exception>
    Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default);
}
