using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Common;

/// <summary>
/// Concentra las comprobaciones de autorización que repiten todos los casos de
/// uso: exigir una acción habilitada y resolver la empresa sobre la que se opera.
/// </summary>
/// <remarks>
/// Es una fachada de <see cref="PoliticaDeAcceso"/> sobre la identidad de la
/// petición en curso. La regla de negocio sigue en el dominio; esta clase sólo
/// evita que cada manejador repita el mismo par de líneas.
/// </remarks>
public sealed class AutorizadorDeCasosDeUso
{
    private readonly IUsuarioActual _usuario;
    private readonly PoliticaDeAcceso _politica;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AutorizadorDeCasosDeUso"/>.
    /// </summary>
    /// <param name="usuario">Identidad efectiva del solicitante.</param>
    /// <param name="politica">Matriz de permisos.</param>
    public AutorizadorDeCasosDeUso(IUsuarioActual usuario, PoliticaDeAcceso politica)
    {
        _usuario = usuario;
        _politica = politica;
    }

    /// <summary>Obtiene la identidad del solicitante.</summary>
    /// <value>La misma instancia inyectada en el ámbito.</value>
    public IUsuarioActual Usuario => _usuario;

    /// <summary>
    /// Indica si el solicitante opera sobre todas las empresas.
    /// </summary>
    /// <value><c>true</c> para operador de nómina y administrador.</value>
    public bool EsTransversal => _politica.OperaSobreTodasLasEmpresas(_usuario.Rol);

    /// <summary>
    /// Comprueba que el solicitante tiene habilitada una acción.
    /// </summary>
    /// <param name="accion">Acción que se pretende ejecutar.</param>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si la acción no está habilitada.</exception>
    public void Exigir(AccionDelSistema accion)
        => _politica.GarantizarPuedeEjecutar(_usuario.Rol, _usuario.Permisos, accion);

    /// <summary>
    /// Indica si el solicitante tiene habilitada una acción.
    /// </summary>
    /// <param name="accion">Acción consultada.</param>
    /// <returns><c>true</c> si está habilitada.</returns>
    public bool Puede(AccionDelSistema accion)
        => _politica.PuedeEjecutar(_usuario.Rol, _usuario.Permisos, accion);

    /// <summary>
    /// Resuelve la empresa sobre la que opera la petición.
    /// </summary>
    /// <param name="empresaSolicitada">Empresa indicada por el cliente, si la hay.</param>
    /// <returns>La empresa efectiva.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si un usuario de empresa cliente pide otra empresa o si un rol transversal no la indica.
    /// </exception>
    public Guid ResolverEmpresa(Guid? empresaSolicitada)
        => _politica.ResolverEmpresaObjetivo(_usuario.Rol, _usuario.EmpresaId, _usuario.Empresas, empresaSolicitada);

    /// <summary>
    /// Resuelve la empresa cuando la petición admite un ámbito transversal.
    /// </summary>
    /// <param name="empresaSolicitada">Empresa indicada por el cliente, si la hay.</param>
    /// <returns>
    /// Para la empresa cliente, la solicitada si es suya o su empresa principal;
    /// para un rol transversal, la solicitada o <c>null</c> para todas.
    /// </returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si un usuario de empresa cliente pide una empresa que no es suya.
    /// </exception>
    public Guid? ResolverEmpresaOpcional(Guid? empresaSolicitada)
        => EsTransversal ? empresaSolicitada : ResolverEmpresa(empresaSolicitada);

    /// <summary>
    /// Resuelve las empresas de una consulta que puede abarcar varias.
    /// </summary>
    /// <param name="empresaSolicitada">Empresa elegida, o <c>null</c> para todas las que le corresponden.</param>
    /// <returns>
    /// La empresa elegida, si la hay; si no, <c>null</c> (todas) para un rol
    /// transversal o todas las empresas del usuario de empresa cliente.
    /// </returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si un usuario de empresa cliente pide una empresa que no es suya
    /// o no está vinculado a ninguna.
    /// </exception>
    public IReadOnlyList<Guid>? ResolverEmpresasDelAmbito(Guid? empresaSolicitada)
    {
        if (empresaSolicitada is not null)
        {
            return [ResolverEmpresa(empresaSolicitada)];
        }

        if (EsTransversal)
        {
            return null;
        }

        return _usuario.Empresas.Count > 0
            ? _usuario.Empresas
            : throw new AccesoNoAutorizadoException("El usuario no está vinculado a ninguna empresa.");
    }

    /// <summary>
    /// Calcula las acciones efectivas del solicitante.
    /// </summary>
    /// <returns>Conjunto de acciones habilitadas.</returns>
    public IReadOnlySet<AccionDelSistema> AccionesEfectivas()
        => _politica.AccionesEfectivas(_usuario.Rol, _usuario.Permisos);

    /// <summary>
    /// Calcula las acciones efectivas de cualquier usuario.
    /// </summary>
    /// <param name="usuario">Usuario consultado.</param>
    /// <returns>Conjunto de acciones habilitadas.</returns>
    public IReadOnlySet<AccionDelSistema> AccionesEfectivasDe(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        return _politica.AccionesEfectivas(usuario.Rol, usuario.Permisos);
    }
}
