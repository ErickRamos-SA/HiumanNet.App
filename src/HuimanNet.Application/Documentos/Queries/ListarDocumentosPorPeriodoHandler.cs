using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Documentos.Queries;

/// <summary>
/// Ejecuta <see cref="ListarDocumentosPorPeriodoQuery"/> resolviendo primero la
/// empresa sobre la que el solicitante tiene derecho a operar.
/// </summary>
public sealed class ListarDocumentosPorPeriodoHandler
    : IManejadorDeConsulta<ListarDocumentosPorPeriodoQuery, IReadOnlyList<DocumentoDto>>
{
    private readonly IConsultasDocumentos _consultas;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarDocumentosPorPeriodoHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de documentos.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol y tipo.</param>
    public ListarDocumentosPorPeriodoHandler(
        IConsultasDocumentos consultas,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="Domain.Exceptions.AccesoNoAutorizadoException">
    /// Se lanza si el solicitante pide documentos de una empresa que no le corresponde.
    /// </exception>
    public async Task<IReadOnlyList<DocumentoDto>> EjecutarAsync(
        ListarDocumentosPorPeriodoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        Guid empresaId = _politicaDeAcceso.ResolverEmpresaObjetivo(
            _usuarioActual.Rol, _usuarioActual.EmpresaId, _usuarioActual.Empresas, consulta.EmpresaId);

        return await _consultas.ListarPorPeriodoAsync(
            consulta.PeriodoId,
            empresaId,
            consulta.Tipo,
            consulta.SoloDescargables,
            cancellationToken);
    }
}
