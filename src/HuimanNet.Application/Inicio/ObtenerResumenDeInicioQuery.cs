using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Usuarios;

namespace HuimanNet.Application.Inicio;

/// <summary>
/// Obtiene los indicadores del panel de inicio del usuario.
/// </summary>
/// <param name="EmpresaId">
/// Empresa elegida por un usuario de empresa cliente con varias empresas;
/// <c>null</c> para su empresa principal o, en los roles transversales, para todas.
/// </param>
public sealed record ObtenerResumenDeInicioQuery(Guid? EmpresaId = null);

/// <summary>
/// Ejecuta <see cref="ObtenerResumenDeInicioQuery"/> acotando el ámbito a una
/// empresa del usuario o, para roles transversales, a todas.
/// </summary>
public sealed class ObtenerResumenDeInicioHandler : IManejadorDeConsulta<ObtenerResumenDeInicioQuery, ResumenDeInicioDto>
{
    private readonly IConsultasInicio _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ObtenerResumenDeInicioHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura del panel.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ObtenerResumenDeInicioHandler(IConsultasInicio consultas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public Task<ResumenDeInicioDto> EjecutarAsync(ObtenerResumenDeInicioQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        return _consultas.ObtenerResumenAsync(_autorizador.ResolverEmpresaOpcional(consulta.EmpresaId), cancellationToken);
    }
}
