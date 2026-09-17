using System.Data;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Services;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using HuimanNet.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de lectura de usuarios para la administración.
/// </summary>
public sealed class ConsultasUsuarios : RepositorioSqlBase, IConsultasUsuarios
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultasUsuarios"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public ConsultasUsuarios(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Usuarios.ListarParaAdministracion, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = (object?)empresaId ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@IncluirInactivos", SqlDbType.Bit) { Value = incluirInactivos });
        comando.Parameters.Add(new SqlParameter("@Sistema", SqlDbType.UniqueIdentifier) { Value = IdentidadesDelSistema.Sistema });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        IReadOnlyList<Usuario> usuarios = await LectorDeUsuarios.LeerAsync(reader, cancellationToken);

        // Cuarto conjunto: el nombre de la empresa principal de cada usuario.
        var empresas = new Dictionary<Guid, string>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                empresas[reader.GetGuid(0)] = reader.GetString(1);
            }
        }

        return usuarios
            .Select(u => Mapeadores.ADto(
                u,
                u.EmpresaId is { } id && empresas.TryGetValue(id, out string? nombre) ? nombre : null,
                PermisosPorRol.Efectivas(u.Rol, u.Permisos)))
            .ToList();
    }
}
