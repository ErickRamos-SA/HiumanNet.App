using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Usuarios;

/// <summary>Crea o actualiza un usuario.</summary>
/// <param name="UsuarioId">Usuario a actualizar, o <c>null</c> para crear.</param>
/// <param name="Datos">Datos del usuario.</param>
public sealed record GuardarUsuarioCommand(Guid? UsuarioId, GuardarUsuarioRequest Datos);

/// <summary>Restablece la contraseña local de un usuario (administrador).</summary>
/// <param name="UsuarioId">Usuario afectado.</param>
/// <param name="NuevaContrasena">Contraseña temporal.</param>
public sealed record RestablecerContrasenaCommand(Guid UsuarioId, string NuevaContrasena);

/// <summary>Lista los usuarios.</summary>
/// <param name="EmpresaId">Empresa a filtrar, o <c>null</c>.</param>
/// <param name="IncluirInactivos">Si se incluyen los desactivados.</param>
public sealed record ListarUsuariosQuery(Guid? EmpresaId, bool IncluirInactivos);

/// <summary>Obtiene un usuario para la administración.</summary>
/// <param name="UsuarioId">Usuario consultado.</param>
public sealed record ObtenerUsuarioQuery(Guid UsuarioId);

/// <summary>Inicia sesión con credenciales locales.</summary>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Contrasena">Contraseña en claro.</param>
/// <param name="DireccionIp">Dirección IP de origen, para la bitácora.</param>
public sealed record IniciarSesionLocalCommand(string Correo, string Contrasena, string? DireccionIp);

/// <summary>Cambia la contraseña del propio usuario.</summary>
/// <param name="ContrasenaActual">Contraseña vigente.</param>
/// <param name="NuevaContrasena">Nueva contraseña.</param>
public sealed record CambiarContrasenaCommand(string ContrasenaActual, string NuevaContrasena);

/// <summary>Actualiza las preferencias del propio usuario.</summary>
/// <param name="Idioma">Idioma preferido.</param>
public sealed record ActualizarPreferenciasCommand(Idioma Idioma);

/// <summary>Obtiene la identidad efectiva del solicitante.</summary>
public sealed record ObtenerUsuarioActualQuery;

/// <summary>
/// Ejecuta la administración de usuarios: alta, actualización, permisos y
/// restablecimiento de contraseña.
/// </summary>
/// <remarks>
/// Reservado al administrador. En modo Entra el alta pre-aprovisiona al
/// usuario (rol, empresa y permisos) y el primer inicio de sesión lo enlaza por
/// correo; en modo local el alta incluye la contraseña inicial, que el usuario
/// debe cambiar en su primer acceso.
/// </remarks>
public sealed class AdministrarUsuariosHandler
    : IManejadorDeComando<GuardarUsuarioCommand, UsuarioDto>,
      IManejadorDeComando<RestablecerContrasenaCommand>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEmpresaRepository _empresas;
    private readonly IHasherDeContrasenas _hasher;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AdministrarUsuariosHandler"/>.
    /// </summary>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="empresas">Repositorio de empresas.</param>
    /// <param name="hasher">Derivación de contraseñas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public AdministrarUsuariosHandler(
        IUsuarioRepository usuarios,
        IEmpresaRepository empresas,
        IHasherDeContrasenas hasher,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _usuarios = usuarios;
        _empresas = empresas;
        _hasher = hasher;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<UsuarioDto> EjecutarAsync(GuardarUsuarioCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        _autorizador.Exigir(AccionDelSistema.AdministrarUsuarios);

        GuardarUsuarioRequest d = comando.Datos;

        if (string.IsNullOrWhiteSpace(d.Correo) || !d.Correo.Contains('@', StringComparison.Ordinal))
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(d.Correo), "Debe indicarse un correo válido.")]);
        }

        Empresa? empresa = null;

        if (d.EmpresaId is not null)
        {
            empresa = await _empresas.ObtenerPorIdAsync(d.EmpresaId.Value, cancellationToken)
                ?? throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(d.EmpresaId), "La empresa indicada no existe.")]);
        }

        // Empresas adicionales: sólo para la empresa cliente, sin repetir la principal.
        List<Guid> adicionales = d.Rol == RolUsuario.ClienteEmpresa
            ? [.. (d.EmpresasAdicionales ?? []).Where(e => e != d.EmpresaId).Distinct()]
            : [];

        foreach (Guid adicional in adicionales)
        {
            _ = await _empresas.ObtenerPorIdAsync(adicional, cancellationToken)
                ?? throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(d.EmpresasAdicionales), "Una de las empresas adicionales no existe.")]);
        }

        Usuario? existentePorCorreo = await _usuarios.ObtenerPorCorreoAsync(d.Correo, cancellationToken);

        if (existentePorCorreo is not null && existentePorCorreo.Id != comando.UsuarioId)
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(d.Correo), "Ya existe un usuario con ese correo.")]);
        }

        // Separación de funciones: procesar la nómina y administrar no se le
        // concede a un usuario de empresa cliente ni con un permiso personalizado.
        PermisoDto? noConcedible = (d.Permisos ?? [])
            .FirstOrDefault(p => p.Habilitado && !HuimanNet.Domain.Services.PermisosPorRol.PuedeConcederse(d.Rol, p.Accion));

        if (noConcedible is not null)
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(
                nameof(d.Permisos),
                $"La acción '{noConcedible.Accion}' es exclusiva de nómina y administración; no puede concederse a un usuario de empresa cliente.")]);
        }

        IEnumerable<PermisoDeUsuario> permisos = (d.Permisos ?? []).Select(static p => new PermisoDeUsuario(p.Accion, p.Habilitado));
        Usuario usuario;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.UsuarioId is null)
        {
            if (!string.IsNullOrEmpty(d.ContrasenaInicial))
            {
                string? error = PoliticaDeContrasenas.Validar(d.ContrasenaInicial);

                if (error is not null)
                {
                    throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(d.ContrasenaInicial), error)]);
                }

                usuario = Usuario.CrearLocal(
                    d.NombreCompleto, d.Correo, d.Rol, d.EmpresaId, _hasher.Hashear(d.ContrasenaInicial), _reloj.GetUtcNow(), d.Idioma);
            }
            else
            {
                usuario = Usuario.Crear(
                    Usuario.PrefijoLocal + d.Correo.Trim().ToLowerInvariant(), d.NombreCompleto, d.Correo, d.Rol,
                    d.EmpresaId, _reloj.GetUtcNow(), d.Idioma);
            }

            usuario.AsignarEmpresasAdicionales(adicionales);
            usuario.ReemplazarPermisos(permisos);

            if (!d.Activo)
            {
                usuario.Desactivar();
            }

            await _usuarios.AgregarAsync(usuario, cancellationToken);
        }
        else
        {
            usuario = await _usuarios.ObtenerPorIdAsync(comando.UsuarioId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El usuario '{comando.UsuarioId}' no existe.");

            if (usuario.Id == _autorizador.Usuario.UsuarioId && (d.Rol != RolUsuario.Administrador || !d.Activo))
            {
                throw new NominaInvalidaException("Un administrador no puede quitarse a sí mismo el rol ni desactivarse.");
            }

            usuario.ActualizarPerfil(d.NombreCompleto, d.Correo);
            usuario.AsignarRol(d.Rol, d.EmpresaId);
            usuario.AsignarEmpresasAdicionales(adicionales);
            usuario.CambiarIdioma(d.Idioma);
            usuario.ReemplazarPermisos(permisos);

            if (d.Activo)
            {
                usuario.Activar();
            }
            else
            {
                usuario.Desactivar();
            }

            await _usuarios.ActualizarAsync(usuario, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeUsuario, usuario.EmpresaId, nameof(Usuario), usuario.Id,
            $"{(comando.UsuarioId is null ? "alta" : "actualizacion")}; rol={usuario.Rol}; activo={usuario.Activo}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(usuario, empresa?.RazonSocial, _autorizador.AccionesEfectivasDe(usuario));
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(RestablecerContrasenaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.AdministrarUsuarios);

        string? error = PoliticaDeContrasenas.Validar(comando.NuevaContrasena);

        if (error is not null)
        {
            throw new EntradaInvalidaException([new ErrorDeValidacion(nameof(comando.NuevaContrasena), error)]);
        }

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(comando.UsuarioId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El usuario '{comando.UsuarioId}' no existe.");

        usuario.EstablecerContrasena(_hasher.Hashear(comando.NuevaContrasena), requiereCambio: true);

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _usuarios.ActualizarAsync(usuario, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeUsuario, usuario.EmpresaId, nameof(Usuario), usuario.Id, "restablecimiento-contrasena", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }
}

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

/// <summary>
/// Ejecuta los casos de uso del propio usuario: inicio de sesión local, cambio
/// de contraseña, preferencias e identidad efectiva.
/// </summary>
public sealed class SesionDeUsuarioHandler
    : IManejadorDeComando<IniciarSesionLocalCommand, IniciarSesionResponse>,
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

        Usuario? usuario = string.IsNullOrWhiteSpace(comando.Correo)
            ? null
            : await _usuarios.ObtenerPorCorreoAsync(comando.Correo, cancellationToken);

        bool valido = usuario is not null
            && usuario.Activo
            && usuario.HashContrasena is not null
            && !string.IsNullOrEmpty(comando.Contrasena)
            && _hasher.Verificar(comando.Contrasena, usuario.HashContrasena);

        if (!valido)
        {
            await _auditoria.AgregarAsync(
                RegistroAuditoria.Fallido(
                    AccionAuditada.InicioDeSesion, usuario?.Id ?? IdentidadesDelSistema.Sistema, usuario?.EmpresaId,
                    nameof(Usuario), usuario?.Id, _reloj.GetUtcNow(), "credenciales-invalidas", comando.DireccionIp),
                cancellationToken);

            _logger.LogWarning("Inicio de sesión local rechazado.");
            throw new AccesoNoAutorizadoException("Credenciales no válidas.");
        }

        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.InicioDeSesion, usuario!.Id, usuario.EmpresaId, nameof(Usuario), usuario.Id,
                _reloj.GetUtcNow(), null, comando.DireccionIp),
            cancellationToken);

        TokenEmitido token = _emisor.Emitir(usuario);
        IReadOnlyList<EmpresaDto> empresas = await EmpresasDeAsync(usuario.Empresas, cancellationToken);

        return new IniciarSesionResponse(
            token.Token, token.ExpiraEn, Mapeadores.AUsuarioActual(usuario, empresas, _autorizador.AccionesEfectivasDe(usuario)));
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
