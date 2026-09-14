using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Implementación de <see cref="ISqlConnectionFactory"/> sobre
/// <c>Microsoft.Data.SqlClient</c>.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _cadenaDeConexion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SqlConnectionFactory"/>.
    /// </summary>
    /// <param name="opciones">Opciones de conexión enlazadas desde configuración.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="opciones"/> es <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la cadena de conexión no está configurada.
    /// </exception>
    public SqlConnectionFactory(IOptions<OpcionesDeSqlServer> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        OpcionesDeSqlServer valor = opciones.Value;

        if (string.IsNullOrWhiteSpace(valor.CadenaDeConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{OpcionesDeSqlServer.Seccion}:{nameof(OpcionesDeSqlServer.CadenaDeConexion)}'.");
        }

        _cadenaDeConexion = valor.CadenaDeConexion;
        TiempoDeEsperaComandoSegundos = valor.TiempoDeEsperaComandoSegundos;
    }

    /// <inheritdoc/>
    public int TiempoDeEsperaComandoSegundos { get; }

    /// <inheritdoc/>
    public SqlConnection Crear() => new(_cadenaDeConexion);
}
