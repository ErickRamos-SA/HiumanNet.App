using System.Globalization;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Inicio;

/// <summary>
/// Ejecuta <see cref="ObtenerResumenDeInicioQuery"/>: lee los datos del ámbito
/// del usuario y decide qué pendientes sugerirle.
/// </summary>
/// <remarks>
/// Reglas de los pendientes; cada una exige además la acción correspondiente:
/// <list type="bullet">
///   <item><description>Empresa cliente: subir documentos a un período abierto sin archivos suyos.</description></item>
///   <item><description>Empresa cliente: descargar los resultados de un período que ya los tiene.</description></item>
///   <item><description>Nómina y administración: procesar un período que recibió los documentos.</description></item>
///   <item><description>Nómina y administración: cotejar una corrida calculada.</description></item>
/// </list>
/// </remarks>
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
    public async Task<ResumenDeInicioDto> EjecutarAsync(ObtenerResumenDeInicioQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        DatosDeInicio datos = await _consultas.ObtenerDatosAsync(
            _autorizador.ResolverEmpresaOpcional(consulta.EmpresaId), cancellationToken);

        return new ResumenDeInicioDto(
            datos.EmpleadosActivos, datos.PeriodosAbiertos, datos.DocumentosDisponibles, datos.CorridasPorCotejar, Pendientes(datos));
    }

    /// <summary>Decide los pendientes del usuario a partir de los datos leídos.</summary>
    /// <param name="datos">Datos del panel.</param>
    /// <returns>Los pendientes, primero los de períodos y después los de corridas.</returns>
    private List<PendienteDto> Pendientes(DatosDeInicio datos)
    {
        bool transversal = _autorizador.EsTransversal;
        var pendientes = new List<PendienteDto>();

        foreach (PeriodoDeInicio periodo in datos.Periodos)
        {
            if (TipoDe(periodo, transversal) is { } tipo)
            {
                pendientes.Add(new PendienteDto(
                    tipo, $"{periodo.Descripcion} · {periodo.EmpresaRazonSocial}", periodo.EmpresaId, periodo.PeriodoId, CorridaId: null));
            }
        }

        if (transversal && _autorizador.Puede(AccionDelSistema.CotejarNomina))
        {
            foreach (CorridaDeInicio corrida in datos.Corridas)
            {
                pendientes.Add(new PendienteDto(
                    TipoDePendiente.CotejarCorrida,
                    string.Create(CultureInfo.InvariantCulture, $"#{corrida.Numero} · {corrida.DescripcionPeriodo}"),
                    corrida.EmpresaId,
                    PeriodoId: null,
                    corrida.CorridaId));
            }
        }

        return pendientes;
    }

    /// <summary>Decide qué pendiente genera un período para el usuario.</summary>
    /// <param name="periodo">Período no cerrado.</param>
    /// <param name="transversal">Si el usuario es de nómina o administración.</param>
    /// <returns>El tipo de pendiente, o <c>null</c> si el período no requiere nada del usuario.</returns>
    private TipoDePendiente? TipoDe(PeriodoDeInicio periodo, bool transversal)
    {
        if (transversal)
        {
            return periodo.Estado == EstadoPeriodo.Recibido && _autorizador.Puede(AccionDelSistema.GestionarPeriodos)
                ? TipoDePendiente.ProcesarPeriodo
                : null;
        }

        if (periodo.Estado == EstadoPeriodo.Abierto && periodo.DocumentosDelCliente == 0
            && _autorizador.Puede(AccionDelSistema.CargarDocumentos))
        {
            return TipoDePendiente.SubirDocumentos;
        }

        return periodo.Estado == EstadoPeriodo.ResultadosDisponibles && periodo.DocumentosDeResultado > 0
               && _autorizador.Puede(AccionDelSistema.DescargarDocumentos)
            ? TipoDePendiente.DescargarResultados
            : null;
    }
}
