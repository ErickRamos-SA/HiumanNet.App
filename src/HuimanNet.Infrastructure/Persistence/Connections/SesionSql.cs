using HuimanNet.Application.Interfaces;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Conexión y transacción compartidas por todos los repositorios de una misma
/// petición.
/// </summary>
public interface ISesionSql
{
    /// <summary>
    /// Obtiene la conexión de la petición, abriéndola la primera vez.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La conexión abierta y compartida.</returns>
    ValueTask<SqlConnection> ObtenerConexionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la transacción abierta, si el caso de uso inició una.
    /// </summary>
    /// <value><c>null</c> cuando las escrituras se ejecutan en confirmación automática.</value>
    SqlTransaction? TransaccionActual { get; }

    /// <summary>
    /// Obtiene el tiempo máximo de ejecución que debe aplicarse a cada comando.
    /// </summary>
    /// <value>Valor en segundos, tomado de la configuración.</value>
    int TiempoDeEsperaComandoSegundos { get; }
}

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

        internal TransaccionSql(SqlTransaction interna, Action alCerrar)
        {
            Interna = interna;
            _alCerrar = alCerrar;
        }

        internal SqlTransaction Interna { get; }

        public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                throw new InvalidOperationException("La transacción ya fue finalizada.");
            }

            await Interna.CommitAsync(cancellationToken);
            _finalizada = true;
        }

        public async Task RevertirAsync(CancellationToken cancellationToken = default)
        {
            if (_finalizada)
            {
                return;
            }

            await Interna.RollbackAsync(cancellationToken);
            _finalizada = true;
        }

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
