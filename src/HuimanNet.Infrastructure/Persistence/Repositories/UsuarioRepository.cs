using System.Data;
using System.Globalization;
using System.Text;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de usuarios, de sus permisos personalizados y de sus empresas
/// adicionales, basado en ADO.NET sobre SQL Server.
/// </summary>
public sealed class UsuarioRepository : RepositorioSqlBase, IUsuarioRepository
{
    /// <summary>
    /// Condición que cumple un usuario que opera en la empresa <c>@EmpresaId</c>,
    /// como principal o adicional.
    /// </summary>
    private const string PerteneceALaEmpresa = """
        (u.EmpresaId = @EmpresaId
         OR EXISTS (SELECT 1 FROM dbo.EmpresasAdicionalesDeUsuario AS a WHERE a.UsuarioId = u.Id AND a.EmpresaId = @EmpresaId))
        """;

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
        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas} FROM dbo.Usuarios AS u WHERE u.Id = @Id;
            SELECT {LectorDeUsuarios.ColumnasDePermiso} FROM dbo.PermisosDeUsuario AS p WHERE p.UsuarioId = @Id;
            SELECT {LectorDeUsuarios.ColumnasDeEmpresaAdicional} FROM dbo.EmpresasAdicionalesDeUsuario AS x WHERE x.UsuarioId = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Usuario?> ObtenerPorIdentificadorExternoAsync(
        string identificadorExterno, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorExterno);

        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas} FROM dbo.Usuarios AS u WHERE u.IdentificadorExterno = @Valor;
            SELECT {LectorDeUsuarios.ColumnasDePermiso} FROM dbo.PermisosDeUsuario AS p
            WHERE p.UsuarioId IN (SELECT Id FROM dbo.Usuarios WHERE IdentificadorExterno = @Valor);
            SELECT {LectorDeUsuarios.ColumnasDeEmpresaAdicional} FROM dbo.EmpresasAdicionalesDeUsuario AS x
            WHERE x.UsuarioId IN (SELECT Id FROM dbo.Usuarios WHERE IdentificadorExterno = @Valor);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Valor", SqlDbType.NVarChar, 128) { Value = identificadorExterno.Trim() });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);

        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas} FROM dbo.Usuarios AS u WHERE u.Correo = @Valor;
            SELECT {LectorDeUsuarios.ColumnasDePermiso} FROM dbo.PermisosDeUsuario AS p
            WHERE p.UsuarioId IN (SELECT Id FROM dbo.Usuarios WHERE Correo = @Valor);
            SELECT {LectorDeUsuarios.ColumnasDeEmpresaAdicional} FROM dbo.EmpresasAdicionalesDeUsuario AS x
            WHERE x.UsuarioId IN (SELECT Id FROM dbo.Usuarios WHERE Correo = @Valor);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Valor", SqlDbType.NVarChar, 256) { Value = correo.Trim().ToLowerInvariant() });

        return await LeerUnoAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Usuario>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        string filtro = $"""
            u.Id <> '00000000-0000-0000-0000-000000000001'
              AND (@EmpresaId IS NULL OR {PerteneceALaEmpresa})
              AND (@IncluirInactivos = 1 OR u.Activo = 1)
            """;

        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas} FROM dbo.Usuarios AS u WHERE {filtro} ORDER BY u.NombreCompleto;
            SELECT {LectorDeUsuarios.ColumnasDePermiso} FROM dbo.PermisosDeUsuario AS p
            INNER JOIN dbo.Usuarios AS u ON u.Id = p.UsuarioId WHERE {filtro};
            SELECT {LectorDeUsuarios.ColumnasDeEmpresaAdicional} FROM dbo.EmpresasAdicionalesDeUsuario AS x
            INNER JOIN dbo.Usuarios AS u ON u.Id = x.UsuarioId WHERE {filtro};
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@IncluirInactivos", SqlDbType.Bit) { Value = incluirInactivos });

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
        // El operador y el administrador no pertenecen a ninguna empresa: para
        // esos roles el filtro de empresa no aplica. La empresa cliente recibe
        // los avisos de su empresa principal y de las adicionales.
        string sql = $"""
            SELECT {LectorDeUsuarios.Columnas}
            FROM   dbo.Usuarios AS u
            WHERE  u.Activo = 1 AND u.Rol = @Rol AND (u.EmpresaId IS NULL OR {PerteneceALaEmpresa})
            ORDER BY u.NombreCompleto;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Rol", SqlDbType.TinyInt) { Value = (byte)rol });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await LectorDeUsuarios.LeerAsync(reader, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        const string sql = """
            INSERT INTO dbo.Usuarios
                (Id, IdentificadorExterno, NombreCompleto, Correo, Rol, EmpresaId, Activo, FechaAlta,
                 HashContrasena, RequiereCambioContrasena, Idioma)
            VALUES
                (@Id, @IdentificadorExterno, @NombreCompleto, @Correo, @Rol, @EmpresaId, @Activo, @FechaAlta,
                 @HashContrasena, @RequiereCambio, @Idioma);
            """;

        await using (SqlCommand comando = await CrearComandoAsync(sql, cancellationToken))
        {
            AgregarParametros(comando, usuario);
            comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = usuario.FechaAlta });
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await GuardarPermisosAsync(usuario, cancellationToken);
        await GuardarEmpresasAdicionalesAsync(usuario, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        const string sql = """
            UPDATE dbo.Usuarios
            SET    IdentificadorExterno = @IdentificadorExterno,
                   NombreCompleto = @NombreCompleto,
                   Correo = @Correo,
                   Rol = @Rol,
                   EmpresaId = @EmpresaId,
                   Activo = @Activo,
                   HashContrasena = @HashContrasena,
                   RequiereCambioContrasena = @RequiereCambio,
                   Idioma = @Idioma
            WHERE  Id = @Id;
            """;

        await using (SqlCommand comando = await CrearComandoAsync(sql, cancellationToken))
        {
            AgregarParametros(comando, usuario);
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await GuardarPermisosAsync(usuario, cancellationToken);
        await GuardarEmpresasAdicionalesAsync(usuario, cancellationToken);
    }

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
    }

    /// <summary>
    /// Sustituye los permisos personalizados del usuario en un solo comando.
    /// </summary>
    private async Task GuardarPermisosAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("DELETE FROM dbo.PermisosDeUsuario WHERE UsuarioId = @UsuarioId;");

        if (usuario.Permisos.Count > 0)
        {
            sql.Append(" INSERT INTO dbo.PermisosDeUsuario (UsuarioId, Accion, Habilitado) VALUES ");

            for (int i = 0; i < usuario.Permisos.Count; i++)
            {
                sql.Append(i == 0 ? string.Empty : ", ")
                   .Append(CultureInfo.InvariantCulture, $"(@UsuarioId, @A{i}, @H{i})");
            }

            sql.Append(';');
        }

        await using SqlCommand comando = await CrearComandoAsync(sql.ToString(), cancellationToken);
        comando.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.UniqueIdentifier) { Value = usuario.Id });

        for (int i = 0; i < usuario.Permisos.Count; i++)
        {
            comando.Parameters.Add(new SqlParameter(string.Create(CultureInfo.InvariantCulture, $"@A{i}"), SqlDbType.TinyInt) { Value = (byte)usuario.Permisos[i].Accion });
            comando.Parameters.Add(new SqlParameter(string.Create(CultureInfo.InvariantCulture, $"@H{i}"), SqlDbType.Bit) { Value = usuario.Permisos[i].Habilitado });
        }

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Sustituye las empresas adicionales del usuario en un solo comando.
    /// </summary>
    private async Task GuardarEmpresasAdicionalesAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("DELETE FROM dbo.EmpresasAdicionalesDeUsuario WHERE UsuarioId = @UsuarioId;");

        if (usuario.EmpresasAdicionales.Count > 0)
        {
            sql.Append(" INSERT INTO dbo.EmpresasAdicionalesDeUsuario (UsuarioId, EmpresaId) VALUES ");

            for (int i = 0; i < usuario.EmpresasAdicionales.Count; i++)
            {
                sql.Append(i == 0 ? string.Empty : ", ")
                   .Append(CultureInfo.InvariantCulture, $"(@UsuarioId, @E{i})");
            }

            sql.Append(';');
        }

        await using SqlCommand comando = await CrearComandoAsync(sql.ToString(), cancellationToken);
        comando.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.UniqueIdentifier) { Value = usuario.Id });

        for (int i = 0; i < usuario.EmpresasAdicionales.Count; i++)
        {
            comando.Parameters.Add(new SqlParameter(string.Create(CultureInfo.InvariantCulture, $"@E{i}"), SqlDbType.UniqueIdentifier) { Value = usuario.EmpresasAdicionales[i] });
        }

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Usuario?> LeerUnoAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        IReadOnlyList<Usuario> usuarios = await LectorDeUsuarios.LeerAsync(reader, cancellationToken);
        return usuarios.Count == 0 ? null : usuarios[0];
    }
}
