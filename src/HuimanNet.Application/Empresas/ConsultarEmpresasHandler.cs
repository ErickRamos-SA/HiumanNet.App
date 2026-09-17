using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Application.Empresas;

/// <summary>
/// Ejecuta <see cref="ListarEmpresasDetalleQuery"/> y <see cref="ObtenerEmpresaQuery"/>.
/// </summary>
public sealed class ConsultarEmpresasHandler
    : IManejadorDeConsulta<ListarEmpresasDetalleQuery, IReadOnlyList<EmpresaDetalleDto>>,
      IManejadorDeConsulta<ObtenerEmpresaQuery, EmpresaDetalleDto>
{
    private readonly IConsultasEmpresas _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarEmpresasHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de empresas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarEmpresasHandler(IConsultasEmpresas consultas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmpresaDetalleDto>> EjecutarAsync(
        ListarEmpresasDetalleQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        if (!_autorizador.EsTransversal)
        {
            throw new AccesoNoAutorizadoException("Sólo los roles transversales consultan el catálogo de empresas.");
        }

        return await _consultas.ListarDetalleAsync(consulta.SoloActivas, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmpresaDetalleDto> EjecutarAsync(
        ObtenerEmpresaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        return await _consultas.ObtenerDetalleAsync(empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{empresaId}' no existe.");
    }
}
