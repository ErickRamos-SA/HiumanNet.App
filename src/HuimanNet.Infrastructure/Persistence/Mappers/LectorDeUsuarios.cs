using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.Usuarios</c>, <c>dbo.PermisosDeUsuario</c> y
/// <c>dbo.EmpresasAdicionalesDeUsuario</c> a entidades <see cref="Usuario"/>.
/// </summary>
/// <remarks>
/// Mapeo manual por ordinal, sin reflexión. Los permisos llegan en un segundo
/// conjunto de resultados del mismo comando y las empresas adicionales en un
/// tercero, de modo que un usuario completo se lee en un solo viaje a la base
/// de datos. Los conjuntos que falten se tratan como vacíos.
/// </remarks>
public static class LectorDeUsuarios
{
    /// <summary>
    /// Lee todos los usuarios con sus permisos y sus empresas adicionales.
    /// </summary>
    /// <param name="reader">
    /// Lector cuyo primer conjunto son usuarios, el segundo permisos y el
    /// tercero empresas adicionales.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios en el orden del primer conjunto.</returns>
    public static async Task<IReadOnlyList<Usuario>> LeerAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var filas = new List<FilaDeUsuario>();

        while (await reader.ReadAsync(cancellationToken))
        {
            filas.Add(LeerFila(reader));
        }

        var permisos = new Dictionary<Guid, List<PermisoDeUsuario>>();
        var empresas = new Dictionary<Guid, List<Guid>>();

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                Agregar(permisos, reader.GetGuid(0), new PermisoDeUsuario((AccionDelSistema)reader.GetByte(1), reader.GetBoolean(2)));
            }

            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    Agregar(empresas, reader.GetGuid(0), reader.GetGuid(1));
                }
            }
        }

        return filas
            .Select(f => f.Construir(
                permisos.TryGetValue(f.Id, out List<PermisoDeUsuario>? p) ? p : [],
                empresas.TryGetValue(f.Id, out List<Guid>? e) ? e : []))
            .ToList();
    }

    /// <summary>
    /// Lee la fila actual del primer conjunto de resultados.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>Los datos del usuario, a falta de sus permisos y empresas adicionales.</returns>
    public static FilaDeUsuario LeerFila(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new FilaDeUsuario(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            (RolUsuario)reader.GetByte(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.GetBoolean(6),
            reader.GetDateTimeOffset(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetBoolean(9),
            (Idioma)reader.GetByte(10));
    }

    /// <summary>Añade un valor a la lista de un usuario; la crea si no existe.</summary>
    /// <typeparam name="T">Permiso o empresa adicional.</typeparam>
    /// <param name="destino">Listas por usuario.</param>
    /// <param name="usuarioId">Usuario al que pertenece el valor.</param>
    /// <param name="valor">Valor leído.</param>
    private static void Agregar<T>(Dictionary<Guid, List<T>> destino, Guid usuarioId, T valor)
    {
        if (!destino.TryGetValue(usuarioId, out List<T>? lista))
        {
            lista = [];
            destino[usuarioId] = lista;
        }

        lista.Add(valor);
    }
}
