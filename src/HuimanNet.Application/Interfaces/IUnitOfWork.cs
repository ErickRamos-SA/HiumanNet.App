namespace HuimanNet.Application.Interfaces;

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
