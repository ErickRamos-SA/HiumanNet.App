using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Notificaciones;

/// <summary>
/// Ejecuta <see cref="ListarDestinatariosDeAvisoQuery"/>: decide qué rol recibe
/// cada tipo de aviso y devuelve el correo de los usuarios activos con ese rol.
/// </summary>
/// <remarks>
/// Los documentos recibidos se avisan al operador de nómina; los resultados
/// disponibles, a la empresa cliente; un archivo en cuarentena, al
/// administrador. La regla vive en la capa de aplicación para que cualquier
/// anfitrión que envíe avisos la aplique igual; el anfitrión sólo redacta y
/// envía el correo.
/// </remarks>
public sealed class ListarDestinatariosDeAvisoHandler
    : IManejadorDeConsulta<ListarDestinatariosDeAvisoQuery, IReadOnlyList<string>>
{
    private readonly IUsuarioRepository _usuarios;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarDestinatariosDeAvisoHandler"/>.
    /// </summary>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    public ListarDestinatariosDeAvisoHandler(IUsuarioRepository usuarios) => _usuarios = usuarios;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    public async Task<IReadOnlyList<string>> EjecutarAsync(
        ListarDestinatariosDeAvisoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        IReadOnlyList<Usuario> destinatarios = await _usuarios.ListarDestinatariosAsync(
            consulta.EmpresaId, RolDestinatario(consulta.Tipo), cancellationToken);

        return [.. destinatarios.Select(static u => u.Correo)];
    }

    /// <summary>
    /// Indica qué rol recibe un tipo de aviso.
    /// </summary>
    /// <param name="tipo">Motivo del aviso.</param>
    /// <returns>El rol de los destinatarios.</returns>
    public static RolUsuario RolDestinatario(TipoDeAviso tipo) => tipo switch
    {
        TipoDeAviso.DocumentosRecibidos => RolUsuario.OperadorNomina,
        TipoDeAviso.ResultadosDisponibles => RolUsuario.ClienteEmpresa,
        _ => RolUsuario.Administrador,
    };
}
