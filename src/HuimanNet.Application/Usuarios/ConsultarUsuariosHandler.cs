using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Ejecuta las consultas de usuarios para la administración.
/// </summary>
public sealed class ConsultarUsuariosHandler
    : IManejadorDeConsulta<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>,
      IManejadorDeConsulta<ObtenerUsuarioQuery, UsuarioDto>
{
    private readonly IConsultasUsuarios _consultas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IConsultasEmpresas _empresas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarUsuariosHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de usuarios.</param>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="empresas">Lado de lectura de empresas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarUsuariosHandler(
        IConsultasUsuarios consultas, IUsuarioRepository usuarios, IConsultasEmpresas empresas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _usuarios = usuarios;
        _empresas = empresas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<UsuarioDto>> EjecutarAsync(ListarUsuariosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.AdministrarUsuarios);
        return _consultas.ListarAsync(consulta.EmpresaId, consulta.IncluirInactivos, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UsuarioDto> EjecutarAsync(ObtenerUsuarioQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.AdministrarUsuarios);

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(consulta.UsuarioId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El usuario '{consulta.UsuarioId}' no existe.");

        EmpresaDto? empresa = usuario.EmpresaId is { } empresaId ? await _empresas.ObtenerAsync(empresaId, cancellationToken) : null;

        return Mapeadores.ADto(usuario, empresa?.RazonSocial, _autorizador.AccionesEfectivasDe(usuario));
    }
}
