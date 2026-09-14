using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Empresas.Queries;

/// <summary>
/// Consulta el catálogo de empresas cliente.
/// </summary>
/// <param name="SoloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
public sealed record ListarEmpresasQuery(bool SoloActivas);

/// <summary>
/// Ejecuta <see cref="ListarEmpresasQuery"/>.
/// </summary>
/// <remarks>
/// Reservada a los roles transversales: el catálogo completo de clientes es
/// información comercial que una empresa cliente no debe ver.
/// </remarks>
public sealed class ListarEmpresasHandler
    : IManejadorDeConsulta<ListarEmpresasQuery, IReadOnlyList<EmpresaDto>>
{
    private readonly IConsultasEmpresas _consultas;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarEmpresasHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de empresas.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    public ListarEmpresasHandler(
        IConsultasEmpresas consultas,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol del solicitante no es transversal.
    /// </exception>
    public async Task<IReadOnlyList<EmpresaDto>> EjecutarAsync(
        ListarEmpresasQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        if (!_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{_usuarioActual.Rol}' no puede consultar el catálogo de empresas.");
        }

        return await _consultas.ListarAsync(consulta.SoloActivas, cancellationToken);
    }
}
