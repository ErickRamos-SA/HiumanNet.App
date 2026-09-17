using HuimanNet.Application.Interfaces;

namespace HuimanNet.Application.Notificaciones;

/// <summary>
/// Obtiene los correos a los que debe enviarse un aviso.
/// </summary>
/// <param name="Tipo">Motivo del aviso.</param>
/// <param name="EmpresaId">Empresa a la que se refiere el aviso.</param>
/// <remarks>
/// La consulta el trabajador de avisos, que actúa como el propio sistema: no
/// hay un usuario detrás, por eso no se autoriza contra <see cref="IUsuarioActual"/>.
/// </remarks>
public sealed record ListarDestinatariosDeAvisoQuery(TipoDeAviso Tipo, Guid EmpresaId);
