using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Ejecuta la captura, actualización y eliminación de incidencias.
/// </summary>
/// <remarks>
/// Las incidencias pueden capturarse mientras el período no esté cerrado:
/// el cálculo del sistema se repite en corridas nuevas tantas veces como haga
/// falta durante la etapa de cotejo.
/// </remarks>
public sealed class GuardarIncidenciaHandler
    : IManejadorDeComando<GuardarIncidenciaCommand, IncidenciaDto>,
      IManejadorDeComando<EliminarIncidenciaCommand>
{
    private readonly IIncidenciaRepository _incidencias;
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GuardarIncidenciaHandler"/>.
    /// </summary>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public GuardarIncidenciaHandler(
        IIncidenciaRepository incidencias,
        IPeriodoRepository periodos,
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _incidencias = incidencias;
        _periodos = periodos;
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<IncidenciaDto> EjecutarAsync(GuardarIncidenciaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);

        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        GuardarIncidenciaRequest d = comando.Datos;
        Guid empresaId = _autorizador.ResolverEmpresa(d.EmpresaId);

        PeriodoCarga periodo = await ObtenerPeriodoAbiertoAsync(d.PeriodoId, empresaId, cancellationToken);

        Contrato contrato = await _empleados.ObtenerContratoAsync(d.ContratoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El contrato '{d.ContratoId}' no existe.");

        Empleado empleado = await _empleados.ObtenerPorIdAsync(contrato.EmpleadoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException("El empleado del contrato no existe.");

        RazonSocial? razonSocial = await _razonesSociales.ObtenerPorIdAsync(contrato.RazonSocialId, empresaId, cancellationToken);

        var datos = new DatosDeIncidencia(
            d.DiasPeriodo, d.Vacaciones, d.Ausentismos, d.Incapacidades, d.Festivos, d.HorasDobles, d.HorasTriples,
            d.DomingosTrabajados, d.Gratificacion, d.Reembolsos, d.Teletrabajo, d.Finiquito, d.Cafeteria,
            d.HorasDescontadas, d.OtrosDescuentos, d.PrestamoPersonal, d.Aguinaldo, d.DescuentosFiscales,
            d.FonacotCapturado, d.DescuentoSindicalAdicional, d.AjusteSindical, d.IsrManual, d.TipoDeMovimiento,
            d.Observaciones);

        Incidencia? existente = await _incidencias.ObtenerPorContratoAsync(periodo.Id, contrato.Id, empresaId, cancellationToken);
        DateTimeOffset ahora = _reloj.GetUtcNow();
        Incidencia incidencia;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (existente is null)
        {
            incidencia = Incidencia.Registrar(empresaId, periodo.Id, contrato.Id, datos, _autorizador.Usuario.UsuarioId, ahora);
            await _incidencias.AgregarAsync(incidencia, cancellationToken);
        }
        else
        {
            incidencia = existente;
            incidencia.Actualizar(datos, _autorizador.Usuario.UsuarioId, ahora);
            await _incidencias.ActualizarAsync(incidencia, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(Incidencia), incidencia.Id,
            $"periodo={periodo.Calendario.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return ADto(incidencia, contrato, empleado, razonSocial?.Nombre ?? string.Empty, _autorizador.Usuario.NombreCompleto);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarIncidenciaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        Incidencia incidencia = await _incidencias.ObtenerAsync(comando.IncidenciaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La incidencia '{comando.IncidenciaId}' no existe.");

        await ObtenerPeriodoAbiertoAsync(incidencia.PeriodoId, empresaId, cancellationToken);

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _incidencias.EliminarAsync(incidencia.Id, empresaId, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(Incidencia), incidencia.Id, "eliminacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <summary>Obtiene el período de la empresa y exige que no esté cerrado.</summary>
    /// <param name="periodoId">Período indicado.</param>
    /// <param name="empresaId">Empresa ya resuelta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El período.</returns>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el período no existe para la empresa.</exception>
    /// <exception cref="PeriodoCerradoException">Se lanza si el período está cerrado.</exception>
    private async Task<PeriodoCarga> ObtenerPeriodoAbiertoAsync(Guid periodoId, Guid empresaId, CancellationToken cancellationToken)
    {
        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(periodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{periodoId}' no existe para la empresa.");

        return periodo.Estado == EstadoPeriodo.Cerrado
            ? throw new PeriodoCerradoException(periodo.Id, periodo.Estado)
            : periodo;
    }

    /// <summary>
    /// Proyecta una incidencia a su DTO.
    /// </summary>
    /// <param name="incidencia">Incidencia capturada.</param>
    /// <param name="contrato">Contrato al que pertenece.</param>
    /// <param name="empleado">Empleado del contrato.</param>
    /// <param name="razonSocialNombre">Nombre de la razón social.</param>
    /// <param name="capturadoPor">Nombre de quien capturó.</param>
    /// <returns>El DTO equivalente.</returns>
    public static IncidenciaDto ADto(Incidencia incidencia, Contrato contrato, Empleado empleado, string razonSocialNombre, string capturadoPor)
    {
        ArgumentNullException.ThrowIfNull(incidencia);
        ArgumentNullException.ThrowIfNull(contrato);
        ArgumentNullException.ThrowIfNull(empleado);
        DatosDeIncidencia d = incidencia.Datos;

        return new IncidenciaDto(
            incidencia.Id, incidencia.PeriodoId, contrato.Id, empleado.Id, empleado.Clave, empleado.NombreCompleto,
            razonSocialNombre, contrato.Esquema, d.DiasPeriodo, d.Vacaciones, d.Ausentismos, d.Incapacidades, d.Festivos,
            d.HorasDobles, d.HorasTriples, d.DomingosTrabajados, d.Gratificacion, d.Reembolsos, d.Teletrabajo, d.Finiquito,
            d.Cafeteria, d.HorasDescontadas, d.OtrosDescuentos, d.PrestamoPersonal, d.Aguinaldo, d.DescuentosFiscales,
            d.FonacotCapturado, d.DescuentoSindicalAdicional, d.AjusteSindical, d.IsrManual, d.TipoDeMovimiento,
            d.Observaciones, capturadoPor, incidencia.FechaCaptura);
    }
}
