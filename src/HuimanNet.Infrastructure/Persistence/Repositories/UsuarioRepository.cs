using System.Data;
using HuimanNet.Application.Common;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de usuarios, de sus permisos personalizados y de sus empresas
/// adicionales, basado en ADO.NET sobre SQL Server.
/// </summary>
/// <remarks>
/// Cada lectura trae al usuario con sus permisos y sus empresas adicionales en
/// un solo viaje (tres conjuntos de resultados), y cada escritura sustituye esas
/// colecciones enteras mediante parámetros de tabla.
/// </remarks>
public sealed class UsuarioRepository : RepositorioSqlBase, IUsuarioRepository
{
    /// <summary>Columnas del parámetro de tabla con los permisos personalizados.</summary>
    private static readonly SqlMetaData[] ColumnasDePermiso =
    [
        new("Accion", SqlDbType.TinyInt),
        new("Habilitado", SqlDbType.Bit),
    ];

    /// <summary>Columna del parámetro de tabla con las empresas adicionales.</summary>
    private static readonly SqlMetaData[] ColumnasDeEmpresa =
    [
        new("Id", SqlDbType.UniqueIdentifier),
    ];

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="UsuarioRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public UsuarioRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Usuarios.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Usuario?> ObtenerPorIdentificadorExternoAsync(
        string identificadorExterno, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorExterno);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Usuarios.ObtenerPorIdentificadorExterno, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Valor", SqlDbType.NVarChar, 128) { Value = identificadorExterno.Trim() });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Usuarios.ObtenerPorCorreo, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Valor", SqlDbType.NVarChar, 256) { Value = correo.Trim().ToLowerInvariant() });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Usuario>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Usuarios.Listar, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@IncluirInactivos", SqlDbType.Bit) { Value = incluirInactivos });
        comando.Parameters.Add(new SqlParameter("@Sistema", SqlDbType.UniqueIdentifier) { Value = IdentidadesDelSistema.Sistema });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await LectorDeUsuarios.LeerAsync(reader, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Usuario>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        => ListarAsync(empresaId, incluirInactivos: true, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Usuario>> ListarDestinatariosAsync(
        Guid empresaId, RolUsuario rol, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Usuarios.ListarDestinatarios, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Rol", SqlDbType.TinyInt) { Value = (byte)rol });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await LectorDeUsuarios.LeerAsync(reader, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Usuarios.Insertar, cancellationToken);
        AgregarParametros(comando, usuario);
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = usuario.FechaAlta });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Usuarios.Actualizar, cancellationToken);
        AgregarParametros(comando, usuario);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Agrega los valores de un usuario al comando, incluidos sus permisos y sus
    /// empresas adicionales como parámetros de tabla.
    /// </summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="usuario">Usuario.</param>
    private static void AgregarParametros(SqlCommand comando, Usuario usuario)
    {
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = usuario.Id });
        comando.Parameters.Add(new SqlParameter("@IdentificadorExterno", SqlDbType.NVarChar, 128) { Value = usuario.IdentificadorExterno });
        comando.Parameters.Add(new SqlParameter("@NombreCompleto", SqlDbType.NVarChar, 200) { Value = usuario.NombreCompleto });
        comando.Parameters.Add(new SqlParameter("@Correo", SqlDbType.NVarChar, 256) { Value = usuario.Correo });
        comando.Parameters.Add(new SqlParameter("@Rol", SqlDbType.TinyInt) { Value = (byte)usuario.Rol });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)usuario.EmpresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = usuario.Activo });
        comando.Parameters.Add(new SqlParameter("@HashContrasena", SqlDbType.NVarChar, 400) { Value = (object?)usuario.HashContrasena ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@RequiereCambio", SqlDbType.Bit) { Value = usuario.RequiereCambioDeContrasena });
        comando.Parameters.Add(new SqlParameter("@Idioma", SqlDbType.TinyInt) { Value = (byte)usuario.Idioma });

        comando.Parameters.Add(ParametrosDeTabla.Crear(
            "@Permisos", "dbo.PermisosDeUsuarioTipo", ColumnasDePermiso, usuario.Permisos,
            static (registro, permiso) =>
            {
                registro.SetByte(0, (byte)permiso.Accion);
                registro.SetBoolean(1, permiso.Habilitado);
            }));

        comando.Parameters.Add(ParametrosDeTabla.Crear(
            "@Empresas", "dbo.IdentificadoresTipo", ColumnasDeEmpresa, usuario.EmpresasAdicionales,
            static (registro, empresaId) => registro.SetGuid(0, empresaId)));
    }

    /// <summary>Ejecuta un procedimiento de usuarios y devuelve el primero.</summary>
    /// <param name="comando">Comando ya preparado; devuelve usuarios, permisos y empresas adicionales.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario, o <c>null</c> si no hay filas.</returns>
    private static async Task<Usuario?> LeerUnoAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        IReadOnlyList<Usuario> usuarios = await LectorDeUsuarios.LeerAsync(reader, cancellationToken);
        return usuarios.Count == 0 ? null : usuarios[0];
    }
}
