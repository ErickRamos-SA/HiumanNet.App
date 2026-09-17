using HuimanNet.Contracts.Usuarios;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura de usuarios para la administración.
/// </summary>
public interface IConsultasUsuarios
{
    /// <summary>
    /// Lista los usuarios.
    /// </summary>
    /// <param name="empresaId">Empresa a filtrar, o <c>null</c> para todos.</param>
    /// <param name="incluirInactivos">Si es <c>true</c>, incluye los desactivados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios ordenados por nombre.</returns>
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default);
}
