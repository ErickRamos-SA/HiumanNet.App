using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de documentos basado en ADO.NET sobre SQL Server.
/// Compatible con Native AOT: no utiliza reflexión ni emisión de IL.
/// </summary>
public sealed class DocumentoRepository : RepositorioSqlBase, IDocumentoRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DocumentoRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public DocumentoRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<Documento?> ObtenerPorIdAsync(
        Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Documentos.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Documento?> ObtenerPorIdSinFiltroDeEmpresaAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Documentos.ObtenerSinEmpresa, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Documento?> ObtenerPorRutaBlobAsync(
        string rutaBlob, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaBlob);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Documentos.ObtenerPorRuta, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@RutaBlob", SqlDbType.NVarChar, 400) { Value = rutaBlob });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Documento>> ListarPorPeriodoAsync(
        Guid periodoId,
        Guid empresaId,
        TipoDocumento? tipo = null,
        bool soloDescargables = false,
        CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Documentos.ListarPorPeriodo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.TinyInt)
        {
            Value = tipo is null ? DBNull.Value : (byte)tipo.Value,
        });
        comando.Parameters.Add(new SqlParameter("@SoloDescargables", SqlDbType.Bit) { Value = soloDescargables });
        comando.Parameters.Add(new SqlParameter("@EstadoDisponible", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoDocumento.Disponible,
        });

        var documentos = new List<Documento>();

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            documentos.Add(LectorDeDocumentos.Mapear(reader));
        }

        return documentos;
    }

    /// <inheritdoc/>
    public async Task<int> ContarDisponiblesAsync(
        Guid periodoId, TipoDocumento tipo, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Documentos.ContarDisponibles, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = periodoId });
        comando.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.TinyInt) { Value = (byte)tipo });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt)
        {
            Value = (byte)EstadoDocumento.Disponible,
        });

        object? resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return resultado is long total ? (int)total : 0;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Documento documento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documento);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Documentos.Insertar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = documento.Id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = documento.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@PeriodoId", SqlDbType.UniqueIdentifier) { Value = documento.PeriodoId });
        comando.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.TinyInt) { Value = (byte)documento.Tipo });
        comando.Parameters.Add(new SqlParameter("@NombreOriginal", SqlDbType.NVarChar, 255) { Value = documento.NombreOriginal.Valor });
        comando.Parameters.Add(new SqlParameter("@RutaBlob", SqlDbType.NVarChar, 400) { Value = documento.RutaBlob });
        comando.Parameters.Add(new SqlParameter("@TamanoBytes", SqlDbType.BigInt) { Value = documento.Tamano.Bytes });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)documento.Estado });
        comando.Parameters.Add(HuellaComoParametro(documento));
        comando.Parameters.Add(new SqlParameter("@CargadoPorUsuarioId", SqlDbType.UniqueIdentifier) { Value = documento.CargadoPorUsuarioId });
        comando.Parameters.Add(new SqlParameter("@FechaSolicitud", SqlDbType.DateTimeOffset) { Value = documento.FechaSolicitud });
        comando.Parameters.Add(FechaOpcional("@FechaCargaConfirmada", documento.FechaCargaConfirmada));
        comando.Parameters.Add(FechaOpcional("@FechaEscaneo", documento.FechaEscaneo));
        comando.Parameters.Add(new SqlParameter("@MotivoCuarentena", SqlDbType.NVarChar, 500)
        {
            Value = (object?)documento.MotivoCuarentena ?? DBNull.Value,
        });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Documento documento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documento);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Documentos.Actualizar, cancellationToken);

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = documento.Id });
        comando.Parameters.Add(new SqlParameter("@TamanoBytes", SqlDbType.BigInt) { Value = documento.Tamano.Bytes });
        comando.Parameters.Add(new SqlParameter("@Estado", SqlDbType.TinyInt) { Value = (byte)documento.Estado });
        comando.Parameters.Add(HuellaComoParametro(documento));
        comando.Parameters.Add(FechaOpcional("@FechaCargaConfirmada", documento.FechaCargaConfirmada));
        comando.Parameters.Add(FechaOpcional("@FechaEscaneo", documento.FechaEscaneo));
        comando.Parameters.Add(new SqlParameter("@MotivoCuarentena", SqlDbType.NVarChar, 500)
        {
            Value = (object?)documento.MotivoCuarentena ?? DBNull.Value,
        });

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Crea el parámetro de la huella SHA-256.</summary>
    /// <param name="documento">Documento.</param>
    /// <returns>El parámetro; nulo en la base de datos si aún no hay huella.</returns>
    private static SqlParameter HuellaComoParametro(Documento documento)
        => new("@HuellaSha256", SqlDbType.Char, 64)
        {
            Value = documento.Huella.EstaVacia ? DBNull.Value : documento.Huella.ValorHex,
        };

    /// <summary>Crea un parámetro de fecha opcional.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Fecha, o <c>null</c>.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter FechaOpcional(string nombre, DateTimeOffset? valor)
        => new(nombre, SqlDbType.DateTimeOffset) { Value = (object?)valor ?? DBNull.Value };

    /// <summary>Ejecuta un procedimiento y rehidrata la primera fila.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El documento, o <c>null</c> si no hay filas.</returns>
    private static async Task<Documento?> LeerUnoAsync(
        SqlCommand comando, CancellationToken cancellationToken)
    {
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? LectorDeDocumentos.Mapear(reader)
            : null;
    }
}
