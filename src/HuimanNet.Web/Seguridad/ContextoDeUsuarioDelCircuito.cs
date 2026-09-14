using System.Collections.Frozen;
using System.Security.Claims;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;
using HuimanNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace HuimanNet.Web.Seguridad;

/// <summary>
/// Implementación de <see cref="IUsuarioActual"/> con ámbito de circuito de
/// Blazor Server.
/// </summary>
/// <remarks>
/// En Blazor Server no hay <c>HttpContext</c> fiable durante la vida del
/// circuito: la identidad se obtiene del <see cref="AuthenticationStateProvider"/>
/// y se traduce al usuario local con el mismo resolutor que usa la API. El
/// resultado se memoriza por circuito, junto con las acciones efectivas, las
/// empresas del usuario y el idioma, que se aplica al <see cref="Traductor"/>
/// del circuito.
/// </remarks>
public sealed class ContextoDeUsuarioDelCircuito : IUsuarioActual
{
    private readonly AuthenticationStateProvider _proveedorDeEstado;
    private readonly IServiceScopeFactory _fabricaDeAmbitos;
    private readonly Traductor _traductor;
    private readonly SemaphoreSlim _cerrojo = new(1, 1);

    private Usuario? _usuario;
    private IReadOnlySet<AccionDelSistema> _acciones = FrozenSet<AccionDelSistema>.Empty;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ContextoDeUsuarioDelCircuito"/>.
    /// </summary>
    /// <param name="proveedorDeEstado">Proveedor del estado de autenticación del circuito.</param>
    /// <param name="fabricaDeAmbitos">
    /// Fábrica de ámbitos: la resolución se hace en un ámbito propio para no
    /// retener una conexión SQL durante toda la vida del circuito.
    /// </param>
    /// <param name="traductor">Traductor del circuito.</param>
    public ContextoDeUsuarioDelCircuito(
        AuthenticationStateProvider proveedorDeEstado,
        IServiceScopeFactory fabricaDeAmbitos,
        Traductor traductor)
    {
        _proveedorDeEstado = proveedorDeEstado;
        _fabricaDeAmbitos = fabricaDeAmbitos;
        _traductor = traductor;
    }

    /// <inheritdoc/>
    public Guid UsuarioId => Requerido().Id;

    /// <inheritdoc/>
    public string NombreCompleto => Requerido().NombreCompleto;

    /// <inheritdoc/>
    public string Correo => Requerido().Correo;

    /// <inheritdoc/>
    public RolUsuario Rol => Requerido().Rol;

    /// <inheritdoc/>
    public Guid? EmpresaId => Requerido().EmpresaId;

    /// <inheritdoc/>
    public IReadOnlyList<Guid> Empresas => Requerido().Empresas;

    /// <inheritdoc/>
    public IReadOnlyList<PermisoDeUsuario> Permisos => Requerido().Permisos;

    /// <inheritdoc/>
    public Idioma Idioma => Requerido().Idioma;

    /// <inheritdoc/>
    public bool RequiereCambioDeContrasena => Requerido().RequiereCambioDeContrasena;

    /// <inheritdoc/>
    /// <remarks>En Blazor Server la IP no está disponible de forma fiable durante el circuito.</remarks>
    public string? DireccionIp => null;

    /// <inheritdoc/>
    public bool EstaAutenticado => _usuario is not null;

    /// <summary>
    /// Obtiene la razón social de la empresa principal del usuario.
    /// </summary>
    /// <value><c>null</c> para los roles transversales.</value>
    public string? EmpresaRazonSocial { get; private set; }

    /// <summary>
    /// Obtiene las empresas de un usuario de empresa cliente.
    /// </summary>
    /// <value>La principal primero y después las adicionales; vacía para los roles transversales.</value>
    public IReadOnlyList<EmpresaDto> EmpresasDelUsuario { get; private set; } = [];

    /// <summary>
    /// Obtiene las acciones que el usuario puede ejecutar: las de su rol más
    /// las habilitadas y menos las revocadas por el administrador.
    /// </summary>
    /// <value>Conjunto de acciones efectivas.</value>
    public IReadOnlySet<AccionDelSistema> Acciones => _acciones;

    /// <summary>
    /// Indica si el usuario tiene acceso transversal a todas las empresas.
    /// </summary>
    /// <value><c>true</c> para operadores de nómina y administradores.</value>
    public bool EsTransversal => Rol is RolUsuario.OperadorNomina or RolUsuario.Administrador;

    /// <summary>
    /// Indica si el usuario elige la empresa de trabajo en la cabecera.
    /// </summary>
    /// <value>
    /// <c>true</c> para los roles transversales (entre todas las empresas) y
    /// para la empresa cliente con más de una empresa (entre las suyas).
    /// </value>
    public bool EligeEmpresa => EsTransversal || EmpresasDelUsuario.Count > 1;

    /// <summary>
    /// Obtiene las iniciales del usuario para el avatar.
    /// </summary>
    /// <value>Hasta dos letras en mayúsculas.</value>
    public string Iniciales
    {
        get
        {
            string[] partes = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length switch
            {
                0 => "?",
                1 => partes[0][..1].ToUpperInvariant(),
                _ => string.Concat(partes[0][..1], partes[^1][..1]).ToUpperInvariant(),
            };
        }
    }

    /// <summary>
    /// Indica si el usuario puede ejecutar una acción.
    /// </summary>
    /// <param name="accion">Acción consultada.</param>
    /// <returns><c>true</c> si la acción está entre las efectivas.</returns>
    /// <remarks>Sólo decide qué se muestra: los casos de uso vuelven a autorizar siempre.</remarks>
    public bool Puede(AccionDelSistema accion) => _acciones.Contains(accion);

    /// <summary>
    /// Resuelve la identidad del circuito si todavía no se ha hecho.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el circuito no tiene identidad autenticada o el usuario no
    /// está registrado o está desactivado.
    /// </exception>
    public async Task GarantizarResueltoAsync(CancellationToken cancellationToken = default)
    {
        if (_usuario is not null)
        {
            return;
        }

        await _cerrojo.WaitAsync(cancellationToken);

        try
        {
            if (_usuario is not null)
            {
                return;
            }

            AuthenticationState estado = await _proveedorDeEstado.GetAuthenticationStateAsync();
            ClaimsPrincipal principal = estado.User;

            if (principal.Identity?.IsAuthenticated != true)
            {
                throw new AccesoNoAutorizadoException("El circuito no tiene una identidad autenticada.");
            }

            await using AsyncServiceScope ambito = _fabricaDeAmbitos.CreateAsyncScope();

            Usuario usuario = await ambito.ServiceProvider
                .GetRequiredService<ResolutorDeUsuarioPorClaims>()
                .ResolverAsync(principal, cancellationToken);

            IConsultasEmpresas consultas = ambito.ServiceProvider.GetRequiredService<IConsultasEmpresas>();
            var empresas = new List<EmpresaDto>(usuario.Empresas.Count);

            foreach (Guid empresaId in usuario.Empresas)
            {
                if (await consultas.ObtenerAsync(empresaId, cancellationToken) is { } empresa)
                {
                    empresas.Add(empresa);
                }
            }

            EmpresasDelUsuario = empresas;
            EmpresaRazonSocial = empresas.FirstOrDefault(e => e.Id == usuario.EmpresaId)?.RazonSocial;
            _acciones = PermisosPorRol.Efectivas(usuario.Rol, usuario.Permisos);
            _traductor.Cambiar(usuario.Idioma);
            _usuario = usuario;
        }
        finally
        {
            _cerrojo.Release();
        }
    }

    /// <summary>
    /// Vuelve a leer el usuario de la base de datos (tras cambiar su idioma o contraseña).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task RecargarAsync(CancellationToken cancellationToken = default)
    {
        _usuario = null;
        await GarantizarResueltoAsync(cancellationToken);
    }

    private Usuario Requerido()
        => _usuario ?? throw new InvalidOperationException(
            "La identidad del circuito no está resuelta. ¿La página hereda de ComponenteDelPortal?");
}
