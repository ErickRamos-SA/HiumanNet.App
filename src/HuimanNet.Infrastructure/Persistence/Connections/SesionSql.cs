using HuimanNet.Application.Interfaces;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Implementación de <see cref="ISesionSql"/> y <see cref="IUnitOfWork"/> con
/// ámbito de petición.
/// </summary>
/// <remarks>
/// Con ADO.NET puro no existe un <c>DbContext</c> que coordine las escrituras:
/// esta clase ocupa su lugar. Mantiene una única conexión por petición y, cuando
/// el caso de uso lo pide, una transacción que todos los repositorios comparten
/// (ESPECIFICACION.md §6.1).
/// <para>
/// Debe registrarse con ámbito <c>Scoped</c>: una instancia por petición HTTP o
/// por circuito de Blazor.
/// </para>
/// </remarks>
public sealed class SesionSql : ISesionSql, IUnitOfWork, IAsyncDisposable
{
    private readonly ISqlConnectionFactory _factory;
    private SqlConnection? _conexion;
    private TransaccionSql? _transaccion;
    private bool _liberada;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SesionSql"/>.
    /// </summary>
    /// <param name="factory">Fábrica de conexiones.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="factory"/> es <c>null</c>.
    /// </exception>
    public SesionSql(ISqlConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <inheritdoc/>
    public SqlTransaction? TransaccionActual => _transaccion?.Interna;

    /// <inheritdoc/>
    public int TiempoDeEsperaComandoSegundos => _factory.TiempoDeEsperaComandoSegundos;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Se lanza si la sesión ya fue liberada.</exception>
    public async ValueTask<SqlConnection> ObtenerConexionAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_liberada, this);

        if (_conexion is null)
        {
            _conexion = _factory.Crear();
            await _conexion.OpenAsync(cancellationToken);
        }

        return _conexion;
    }

    /// <inheritdoc/>
    public async Task<ITransaccion> IniciarTransaccionAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_liberada, this);

        if (_transaccion is not null)
        {
            throw new InvalidOperationException(
                "Ya hay una transacción abierta en el ámbito actual.");
        }

        SqlConnection conexion = await ObtenerConexionAsync(cancellationToken);
        SqlTransaction interna = (SqlTransaction)await conexion.BeginTransactionAsync(cancellationToken);

        _transaccion = new TransaccionSql(interna, () => _transaccion = null);
        return _transaccion;
    }

    /// <summary>
    /// Cierra la transacción pendiente y devuelve la conexión al <i>pool</i>.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_liberada)
        {
            return;
        }

        _liberada = true;

        if (_transaccion is not null)
        {
            await _transaccion.DisposeAsync();
        }

        if (_conexion is not null)
        {
            await _conexion.DisposeAsync();
            _conexion = null;
        }
    }

    /// <summary>
    /// Transacción de SQL Server expuesta como <see cref="ITransaccion"/>.
    /// </summary>
    private sealed class TransaccionSql : ITransaccion
    {
        private readonly Action _alCerrar;
        private bool _finalizada;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TransaccionSql"/>.
        /// </summary>
        /// <param name="interna">Transacción de ADO.NET.</param>
        /// <param name="alCerrar">Acción que avisa a la sesión de que ya no hay transacción abierta.</param>
        internal TransaccionSql(SqlTransaction interna, Action alCerrar)
        {
            Interna = interna;
            _alCerrar = alCerrar;
        }

        /// <summary>Obtiene la transacción de ADO.NET que se asigna a los comandos.</summary>
        /// <value>Transacción abierta sobre la conexión de la sesión.</value>
        internal SqlTransaction Interna { get; }

        /// <inheritdoc/>
        public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                throw new InvalidOperationException("La transacción ya fue finalizada.");
            }

            await Interna.CommitAsync(cancellationToken);
            _finalizada = true;
        }

        /// <inheritdoc/>
        public async Task RevertirAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                return;
            }

            await Interna.RollbackAsync(cancellationToken);
            _finalizada = true;
        }

        /// <summary>
        /// Libera la transacción; si no se confirmó, la revierte para no dejar
        /// escrituras parciales.
        /// </summary>
        /// <returns>Tarea que finaliza al liberar la transacción.</returns>
        public async ValueTask DisposeAsync()
        {
            // Liberar sin haber confirmado equivale a revertir: así una excepción
            // a mitad de un caso de uso nunca deja escrituras parciales.
            if (!_finalizada)
            {
                try
                {
                    await Interna.RollbackAsync(CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    // La transacción ya había terminado por el lado del servidor.
                }

                _finalizada = true;
            }

            await Interna.DisposeAsync();
            _alCerrar();
        }
    }
}
