using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Usuarios;

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
            .FirstOrDefault(p => p.Habilitado && !PermisosPorRol.PuedeConcederse(d.Rol, p.Accion));

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
