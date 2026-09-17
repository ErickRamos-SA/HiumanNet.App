using System.Diagnostics;
using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta <see cref="CalcularNominaCommand"/>: resuelve el catálogo vigente,
/// evalúa cada contrato en paralelo y persiste la corrida con sus resultados.
/// </summary>
/// <remarks>
/// <para>
/// El cálculo es una acción manual de nómina o administración, posterior a la
/// carga de los archivos del período. Su resultado se guarda como una corrida
/// del período y la empresa, de modo que consultarlo no vuelve a calcular.
/// Ejecutarlo de nuevo es un <b>reproceso</b>: genera una corrida nueva y marca
/// como reemplazadas las anteriores que no estén aprobadas. Una vez aprobada,
/// la nómina del período ya no se reprocesa.
/// </para>
/// Diseñado para empresas grandes:
/// <list type="bullet">
///   <item><description>Todas las entradas (contratos, razones sociales, incidencias, catálogo) se cargan con una consulta por tipo, nunca por trabajador.</description></item>
///   <item><description>Las fórmulas se compilan una vez por esquema y se evalúan en paralelo; el motor no comparte estado entre trabajadores.</description></item>
///   <item><description>Los resultados se insertan en lote dentro de una sola transacción.</description></item>
/// </list>
/// Un error de catálogo aborta la corrida completa; un error en un contrato
/// concreto se registra como advertencia de ese contrato y no detiene al resto.
/// </remarks>
public sealed class CalcularNominaHandler : IManejadorDeComando<CalcularNominaCommand, CorridaDeNominaDto>
{
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly IIncidenciaRepository _incidencias;
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly IConsultasNomina _consultas;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly MotorDeCalculo _motor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;
    private readonly ILogger<CalcularNominaHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CalcularNominaHandler"/>.
    /// </summary>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="consultas">Lado de lectura de nómina.</param>
    /// <param name="constructor">Resolución del catálogo.</param>
    /// <param name="motor">Motor de cálculo.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    /// <param name="logger">Registro de eventos.</param>
    public CalcularNominaHandler(
        IPeriodoRepository periodos,
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        IIncidenciaRepository incidencias,
        ICorridaDeNominaRepository corridas,
        IConsultasNomina consultas,
        ConstructorDePlanDeCalculo constructor,
        MotorDeCalculo motor,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj,
        ILogger<CalcularNominaHandler> logger)
    {
        _periodos = periodos;
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _incidencias = incidencias;
        _corridas = corridas;
        _consultas = consultas;
        _constructor = constructor;
        _motor = motor;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el período no existe para la empresa o falta el permiso.</exception>
    /// <exception cref="NominaInvalidaException">Se lanza si el período está cerrado o no hay contratos vigentes.</exception>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el catálogo no permite construir el plan.</exception>
    public async Task<CorridaDeNominaDto> EjecutarAsync(
        CalcularNominaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        _autorizador.Exigir(AccionDelSistema.CalcularNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(comando.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{comando.PeriodoId}' no existe para la empresa.");

        // El cálculo es manual y posterior a la carga de los archivos del período.
        periodo.GarantizarQueAdmiteCalculo();

        // Reprocesar genera una corrida nueva; una nómina aprobada ya es definitiva.
        IReadOnlyList<CorridaDeNomina> anteriores = await _corridas.ListarPorPeriodoAsync(periodo.Id, empresaId, cancellationToken);
        CorridaDeNomina? aprobada = anteriores.FirstOrDefault(static c => c.Estado == EstadoDeCorrida.Aprobada);

        if (aprobada is not null)
        {
            throw new NominaInvalidaException(
                $"La corrida #{aprobada.Numero} ya está aprobada como nómina definitiva del período; no puede reprocesarse.");
        }

        DateOnly fecha = periodo.FechaDeReferencia;
        var cronometro = Stopwatch.StartNew();

        // Entradas: una consulta por tipo, nunca por trabajador.
        IReadOnlyList<Contrato> contratos = await _empleados.ListarContratosVigentesAsync(empresaId, fecha, cancellationToken);

        if (contratos.Count == 0)
        {
            throw new NominaInvalidaException("La empresa no tiene contratos vigentes en la fecha del período.");
        }

        IReadOnlyList<Empleado> empleados = await _empleados.ListarPorEmpresaAsync(empresaId, soloActivos: true, cancellationToken);
        IReadOnlyList<RazonSocial> razonesSociales = await _razonesSociales.ListarPorEmpresaAsync(empresaId, soloActivas: false, cancellationToken);
        IReadOnlyList<Incidencia> incidencias = await _incidencias.ListarPorPeriodoAsync(periodo.Id, empresaId, cancellationToken);
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        var empleadosPorId = empleados.ToDictionary(static e => e.Id);
        var razonesPorId = razonesSociales.ToDictionary(static r => r.Id);
        var incidenciasPorContrato = incidencias.ToDictionary(static i => i.ContratoId);

        // Los planes se construyen antes del bucle para que un catálogo
        // inconsistente aborte la corrida con un mensaje claro.
        foreach (EsquemaDePago esquema in contratos.Select(static c => c.Esquema).Distinct())
        {
            catalogo.Plan(esquema);
        }

        int numero = await _corridas.SiguienteNumeroAsync(periodo.Id, cancellationToken);
        CorridaDeNomina corrida = CorridaDeNomina.Iniciar(
            empresaId, periodo.Id, numero, fecha, _autorizador.Usuario.UsuarioId, _reloj.GetUtcNow(), comando.Observaciones);

        var resultados = new ResultadoDeNomina?[contratos.Count];
        var advertencias = new List<string>();
        var cerrojo = new object();

        Parallel.For(
            0,
            contratos.Count,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount },
            i =>
            {
                Contrato contrato = contratos[i];

                if (!empleadosPorId.TryGetValue(contrato.EmpleadoId, out Empleado? empleado))
                {
                    lock (cerrojo)
                    {
                        advertencias.Add($"El contrato {contrato.Id} pertenece a un empleado inactivo o inexistente y se omitió.");
                    }

                    return;
                }

                if (!razonesPorId.TryGetValue(contrato.RazonSocialId, out RazonSocial? razonSocial) || !razonSocial.Activa)
                {
                    lock (cerrojo)
                    {
                        advertencias.Add($"El empleado {empleado.Clave} tiene un contrato con una razón social inactiva o inexistente y se omitió.");
                    }

                    return;
                }

                incidenciasPorContrato.TryGetValue(contrato.Id, out Incidencia? incidencia);
                TipoDeMovimiento movimiento = incidencia?.Datos.TipoDeMovimiento ?? TipoDeMovimiento.Ordinaria;

                try
                {
                    IReadOnlyDictionary<string, decimal> variables =
                        ConstructorDeVariables.Construir(contrato, razonSocial, incidencia, catalogo.Parametros, fecha);

                    ResultadoDeCalculo calculo = _motor.Calcular(catalogo.Plan(contrato.Esquema), variables);
                    IReadOnlyList<string> faltantes = ClavesDeResumen.Faltantes(calculo, contrato.Esquema);
                    string? advertencia = faltantes.Count == 0
                        ? null
                        : "El catálogo no define los conceptos de resumen: " + string.Join(", ", faltantes) + ".";

                    resultados[i] = ResultadoDeNomina.Crear(corrida.Id, contrato, empleado, movimiento, calculo, advertencia);
                }
                catch (DomainException excepcion)
                {
                    resultados[i] = ResultadoDeNomina.Crear(
                        corrida.Id, contrato, empleado, movimiento, ResultadoDeCalculo.Vacio, excepcion.Message);

                    lock (cerrojo)
                    {
                        advertencias.Add($"{empleado.Clave}: {excepcion.Message}");
                    }
                }
            });

        List<ResultadoDeNomina> calculados = resultados.Where(static r => r is not null).Select(static r => r!).ToList();

        if (calculados.Count == 0)
        {
            throw new NominaInvalidaException("Ningún contrato pudo calcularse: " + string.Join(" ", advertencias));
        }

        cronometro.Stop();
        corrida.RegistrarTotales(TotalesDeCorrida.Sumar(calculados), cronometro.ElapsedMilliseconds, advertencias);

        // Las corridas abiertas anteriores quedan reemplazadas: el período conserva
        // un solo resultado vigente y todo el historial para auditoría.
        List<CorridaDeNomina> reemplazadas = anteriores.Where(static c => c.EstaAbierta).ToList();

        foreach (CorridaDeNomina anterior in reemplazadas)
        {
            anterior.Reemplazar(numero);
        }

        string reemplaza = reemplazadas.Count == 0 ? "-" : string.Join(",", reemplazadas.Select(static c => c.Numero));

        await using (ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken))
        {
            await _corridas.AgregarAsync(corrida, calculados, cancellationToken);

            foreach (CorridaDeNomina anterior in reemplazadas)
            {
                await _corridas.ActualizarAsync(anterior, cancellationToken);
            }

            await _auditoria.ExitoAsync(
                AccionAuditada.CalculoDeNomina, empresaId, nameof(CorridaDeNomina), corrida.Id,
                $"periodo={periodo.Calendario.Clave}; numero={numero}; contratos={calculados.Count}; ms={cronometro.ElapsedMilliseconds}; reemplaza={reemplaza}",
                cancellationToken);
            await transaccion.ConfirmarAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Corrida {CorridaId} calculada: {Contratos} contratos en {Milisegundos} ms.",
            corrida.Id, calculados.Count, cronometro.ElapsedMilliseconds);

        return await _consultas.ObtenerCorridaAsync(corrida.Id, empresaId, cancellationToken)
            ?? Mapeadores.ADto(corrida, periodo.Calendario.Clave, periodo.Descripcion, _autorizador.Usuario.NombreCompleto);
    }
}
