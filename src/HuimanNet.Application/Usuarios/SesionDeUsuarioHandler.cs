using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Usuarios;

/// <summary>
/// Ejecuta los casos de uso del propio usuario: inicio de sesión local (con
/// token para la app o con cookie para la web), cambio de contraseña,
/// preferencias e identidad efectiva.
/// </summary>
public sealed class SesionDeUsuarioHandler
    : IManejadorDeComando<IniciarSesionLocalCommand, IniciarSesionResponse>,
      IManejadorDeComando<ValidarCredencialesLocalesCommand, SesionLocalValidada>,
      IManejadorDeComando<CambiarContrasenaCommand>,
      IManejadorDeComando<ActualizarPreferenciasCommand>,
      IManejadorDeConsulta<ObtenerUsuarioActualQuery, UsuarioActualDto>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IConsultasEmpresas _empresas;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IHasherDeContrasenas _hasher;
    private readonly IEmisorDeTokens _emisor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;
    private readonly ILogger<SesionDeUsuarioHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SesionDeUsuarioHandler"/>.
    /// </summary>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="empresas">Lado de lectura de empresas.</param>
    /// <param name="auditoria">Repositorio de la bitácora.</param>
    /// <param name="hasher">Derivación de contraseñas.</param>
    /// <param name="emisor">Emisión de tokens.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public SesionDeUsuarioHandler(
        IUsuarioRepository usuarios,
        IConsultasEmpresas empresas,
        IAuditoriaRepository auditoria,
        IHasherDeContrasenas hasher,
        IEmisorDeTokens emisor,
        AutorizadorDeCasosDeUso autorizador,
        IUnitOfWork unitOfWork,
        TimeProvider reloj,
        ILogger<SesionDeUsuarioHandler> logger)
    {
        _usuarios = usuarios;
        _empresas = empresas;
        _auditoria = auditoria;
        _hasher = hasher;
        _emisor = emisor;
        _autorizador = autorizador;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si las credenciales no son válidas o el usuario está inactivo.</exception>
    public async Task<IniciarSesionResponse> EjecutarAsync(IniciarSesionLocalCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Usuario usuario = await AutenticarAsync(comando.Correo, comando.Contrasena, comando.DireccionIp, cancellationToken);
        TokenEmitido token = _emisor.Emitir(usuario);
        IReadOnlyList<EmpresaDto> empresas = await EmpresasDeAsync(usuario.Empresas, cancellationToken);

        return new IniciarSesionResponse(
            token.Token, token.ExpiraEn, Mapeadores.AUsuarioActual(usuario, empresas, _autorizador.AccionesEfectivasDe(usuario)));
    }

    /// <inheritdoc/>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si las credenciales no son válidas o el usuario está inactivo.</exception>
    public async Task<SesionLocalValidada> EjecutarAsync(ValidarCredencialesLocalesCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Usuario usuario = await AutenticarAsync(comando.Correo, comando.Contrasena, comando.DireccionIp, cancellationToken);

        return new SesionLocalValidada(
            usuario.Id, usuario.IdentificadorExterno, usuario.NombreCompleto, usuario.Correo, usuario.Rol,
            usuario.Idioma, usuario.RequiereCambioDeContrasena);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(CambiarContrasenaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(_autorizador.Usuario.UsuarioId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException("El usuario de la sesión no existe.");

        if (usuario.HashContrasena is null || !_hasher.Verificar(comando.ContrasenaActual ?? string.Empty, usuario.HashContrasena))
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(comando.ContrasenaActual), "La contraseña actual no es correcta.")]);
        }

        string? error = PoliticaDeContrasenas.Validar(comando.NuevaContrasena);

        if (error is not null)
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(comando.NuevaContrasena), error)]);
        }

        usuario.EstablecerContrasena(_hasher.Hashear(comando.NuevaContrasena), requiereCambio: false);

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _usuarios.ActualizarAsync(usuario, cancellationToken);
        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.AdministracionDeUsuario, usuario.Id, usuario.EmpresaId, nameof(Usuario), usuario.Id,
                _reloj.GetUtcNow(), "cambio-contrasena", _autorizador.Usuario.DireccionIp),
            cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(ActualizarPreferenciasCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(_autorizador.Usuario.UsuarioId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException("El usuario de la sesión no existe.");

        usuario.CambiarIdioma(comando.Idioma);
        await _usuarios.ActualizarAsync(usuario, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UsuarioActualDto> EjecutarAsync(ObtenerUsuarioActualQuery consulta, CancellationToken cancellationToken = default)
    {
        IUsuarioActual actual = _autorizador.Usuario;
        IReadOnlyList<EmpresaDto> empresas = await EmpresasDeAsync(actual.Empresas, cancellationToken);

        return Mapeadores.AUsuarioActual(actual, empresas, _autorizador.AccionesEfectivas());
    }

    /// <summary>
    /// Comprueba correo y contraseña de un usuario local y deja constancia del
    /// intento en la bitácora, tanto si prospera como si no.
    /// </summary>
    /// <param name="correo">Correo escrito por el usuario.</param>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <param name="direccionIp">Dirección IP de origen, para la bitácora.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario autenticado.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza con un mensaje único si el correo no existe, la contraseña no
    /// coincide o la cuenta está desactivada: no revela cuál de los tres ocurrió.
    /// </exception>
    private async Task<Usuario> AutenticarAsync(
        string correo, string contrasena, string? direccionIp, CancellationToken cancellationToken)
    {
        Usuario? usuario = string.IsNullOrWhiteSpace(correo)
            ? null
            : await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken);

        bool valido = usuario is not null
            && usuario.Activo
            && usuario.HashContrasena is not null
            && !string.IsNullOrEmpty(contrasena)
            && _hasher.Verificar(contrasena, usuario.HashContrasena);

        if (!valido)
        {
            await _auditoria.AgregarAsync(
                RegistroAuditoria.Fallido(
                    AccionAuditada.InicioDeSesion, usuario?.Id ?? IdentidadesDelSistema.Sistema, usuario?.EmpresaId,
                    nameof(Usuario), usuario?.Id, _reloj.GetUtcNow(), "credenciales-invalidas", direccionIp),
                cancellationToken);

            _logger.LogWarning("Inicio de sesión local rechazado.");
            throw new AccesoNoAutorizadoException("Credenciales no válidas.");
        }

        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.InicioDeSesion, usuario!.Id, usuario.EmpresaId, nameof(Usuario), usuario.Id,
                _reloj.GetUtcNow(), null, direccionIp),
            cancellationToken);

        return usuario;
    }

    /// <summary>
    /// Obtiene los datos de las empresas de un usuario, en el mismo orden.
    /// </summary>
    /// <param name="empresasIds">Empresas del usuario, la principal primero.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas que existen; vacía para los roles transversales.</returns>
    private async Task<IReadOnlyList<EmpresaDto>> EmpresasDeAsync(IReadOnlyList<Guid> empresasIds, CancellationToken cancellationToken)
    {
        var empresas = new List<EmpresaDto>(empresasIds.Count);

        foreach (Guid empresaId in empresasIds)
        {
            if (await _empresas.ObtenerAsync(empresaId, cancellationToken) is { } empresa)
            {
                empresas.Add(empresa);
            }
        }

        return empresas;
    }
}
