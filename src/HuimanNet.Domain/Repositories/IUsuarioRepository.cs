using HuimanNet.Domain.Entities;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de usuarios del portal.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>
    /// Obtiene un usuario por su identificador local.
    /// </summary>
    /// <param name="id">Identificador local del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario encontrado, o <c>null</c> si no existe.</returns>
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un usuario por el identificador del sujeto en el proveedor de identidad.
    /// </summary>
    /// <param name="identificadorExterno">Valor del <i>claim</i> <c>oid</c> o <c>sub</c> del token, o el identificador local.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario encontrado, o <c>null</c> si aún no se ha aprovisionado localmente.</returns>
    Task<Usuario?> ObtenerPorIdentificadorExternoAsync(
        string identificadorExterno, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un usuario por su correo.
    /// </summary>
    /// <param name="correo">Correo, sin distinguir mayúsculas.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario encontrado, o <c>null</c>.</returns>
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los usuarios del portal.
    /// </summary>
    /// <param name="empresaId">Empresa a filtrar, o <c>null</c> para todos.</param>
    /// <param name="incluirInactivos">Si es <c>true</c>, incluye los desactivados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios ordenados por nombre.</returns>
    Task<IReadOnlyList<Usuario>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los usuarios de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa cuyos usuarios se consultan.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios activos e inactivos de la empresa.</returns>
    Task<IReadOnlyList<Usuario>> ListarPorEmpresaAsync(
        Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los usuarios que deben recibir avisos de una acción sobre una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa afectada por la acción.</param>
    /// <param name="rol">Rol de los destinatarios buscados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios activos con ese rol y ámbito.</returns>
    /// <remarks>
    /// El operador de nómina y el administrador no pertenecen a ninguna empresa,
    /// por lo que se devuelven con independencia de <paramref name="empresaId"/>.
    /// </remarks>
    Task<IReadOnlyList<Usuario>> ListarDestinatariosAsync(
        Guid empresaId, Enums.RolUsuario rol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un nuevo usuario con sus permisos personalizados.
    /// </summary>
    /// <param name="usuario">Usuario a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza los datos y los permisos personalizados de un usuario existente.
    /// </summary>
    /// <param name="usuario">Usuario con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
