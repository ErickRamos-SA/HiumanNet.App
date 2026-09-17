using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta <see cref="ListarCorridasRecientesQuery"/>.
/// </summary>
/// <remarks>
/// Aplica las mismas reglas que el listado de corridas de un período: exige la
/// consulta de nómina, acota la empresa al ámbito del usuario y, para la
/// empresa cliente, omite las corridas descartadas, que son historial interno.
/// </remarks>
public sealed class ListarCorridasRecientesHandler
    : IManejadorDeConsulta<ListarCorridasRecientesQuery, IReadOnlyList<CorridaDeNominaDto>>
{
    private readonly IConsultasNomina _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarCorridasRecientesHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de nómina.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ListarCorridasRecientesHandler(IConsultasNomina consultas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el usuario no puede consultar la nómina o pide una empresa que no es suya.
    /// </exception>
    public async Task<IReadOnlyList<CorridaDeNominaDto>> EjecutarAsync(
        ListarCorridasRecientesQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);

        Guid? empresaId = _autorizador.ResolverEmpresaOpcional(consulta.EmpresaId);
        IReadOnlyList<CorridaDeNominaDto> corridas = await _consultas.ListarRecientesAsync(empresaId, consulta.Limite, cancellationToken);

        return _autorizador.EsTransversal
            ? corridas
            : [.. corridas.Where(static c => c.Estado != EstadoDeCorrida.Descartada)];
    }
}
