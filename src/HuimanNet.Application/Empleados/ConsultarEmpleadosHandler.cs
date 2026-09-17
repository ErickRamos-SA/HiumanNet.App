using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Application.Empleados;

/// <summary>
/// Ejecuta <see cref="ListarEmpleadosQuery"/> y <see cref="ObtenerEmpleadoQuery"/>.
/// </summary>
public sealed class ConsultarEmpleadosHandler
    : IManejadorDeConsulta<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>>,
      IManejadorDeConsulta<ObtenerEmpleadoQuery, EmpleadoDto>
{
    private readonly IConsultasEmpleados _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarEmpleadosHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de empleados.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarEmpleadosHandler(IConsultasEmpleados consultas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public Task<PaginaDto<EmpleadoResumenDto>> EjecutarAsync(
        ListarEmpleadosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        // Sin empresa elegida: todas para nómina y administración; todas las
        // suyas (y ninguna otra) para la empresa cliente.
        IReadOnlyList<Guid>? empresas = _autorizador.ResolverEmpresasDelAmbito(consulta.EmpresaId);

        int tamano = Paginacion.NormalizarTamano(
            consulta.TamanoPagina, Paginacion.TamanoDeEmpleados, Paginacion.TamanoMaximoDeEmpleados);

        return _consultas.ListarAsync(
            empresas, consulta.SoloActivos, consulta.Texto, Paginacion.NormalizarPagina(consulta.Pagina), tamano, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmpleadoDto> EjecutarAsync(
        ObtenerEmpleadoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        return await _consultas.ObtenerAsync(consulta.EmpleadoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El empleado '{consulta.EmpleadoId}' no existe.");
    }
}
