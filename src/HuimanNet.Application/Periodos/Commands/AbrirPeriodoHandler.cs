using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Periodos.Commands;

/// <summary>
/// Ejecuta <see cref="AbrirPeriodoCommand"/>.
/// </summary>
/// <remarks>
/// Sólo los roles transversales abren períodos: es el operador de nómina quien
/// marca el inicio del ciclo, no la empresa cliente.
/// </remarks>
public sealed class AbrirPeriodoHandler : IManejadorDeComando<AbrirPeriodoCommand, PeriodoDto>
{
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpresaRepository _empresas;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PoliticaDeAcceso _politicaDeAcceso;
    private readonly IValidadorDeEntrada<AbrirPeriodoCommand> _validadorDeEntrada;
    private readonly TimeProvider _reloj;
    private readonly ILogger<AbrirPeriodoHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AbrirPeriodoHandler"/>.
    /// </summary>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empresas">Repositorio de empresas.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    /// <param name="validadorDeEntrada">Validador de la forma del comando.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public AbrirPeriodoHandler(
        IPeriodoRepository periodos,
        IEmpresaRepository empresas,
        IAuditoriaRepository auditoria,
        IUsuarioActual usuarioActual,
        IUnitOfWork unitOfWork,
        PoliticaDeAcceso politicaDeAcceso,
        IValidadorDeEntrada<AbrirPeriodoCommand> validadorDeEntrada,
        TimeProvider reloj,
        ILogger<AbrirPeriodoHandler> logger)
    {
        _periodos = periodos;
        _empresas = empresas;
        _auditoria = auditoria;
        _usuarioActual = usuarioActual;
        _unitOfWork = unitOfWork;
        _politicaDeAcceso = politicaDeAcceso;
        _validadorDeEntrada = validadorDeEntrada;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="comando"/> es <c>null</c>.</exception>
    /// <exception cref="EntradaInvalidaException">Se lanza si el comando está incompleto o fuera de rango.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol del solicitante no es transversal o si no tiene
    /// habilitada la gestión de períodos.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si los componentes del calendario están fuera de rango o el
    /// período ya existe.
    /// </exception>
    public async Task<PeriodoDto> EjecutarAsync(
        AbrirPeriodoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _validadorDeEntrada.Validar(comando).GarantizarValido();

        if (!_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{_usuarioActual.Rol}' no puede abrir períodos de carga.");
        }

        _politicaDeAcceso.GarantizarPuedeEjecutar(_usuarioActual.Rol, _usuarioActual.Permisos, AccionDelSistema.GestionarPeriodos);

        Empresa empresa = await _empresas.ObtenerPorIdAsync(comando.EmpresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{comando.EmpresaId}' no existe.");

        PeriodoCalendario calendario =
            PeriodoCalendario.Crear(comando.Anio, comando.Mes, comando.Consecutivo);

        PeriodoCarga? existente =
            await _periodos.ObtenerPorCalendarioAsync(empresa.Id, calendario, cancellationToken);

        if (existente is not null)
        {
            throw new DocumentoInvalidoException(
                $"La empresa ya tiene abierto el período '{calendario.Clave}'.");
        }

        DateTimeOffset ahora = _reloj.GetUtcNow();

        PeriodoCarga periodo = PeriodoCarga.Abrir(
            empresa.Id, calendario, comando.Descripcion, ahora, comando.FechaLimiteCarga);

        await using ITransaccion transaccion =
            await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        await _periodos.AgregarAsync(periodo, cancellationToken);

        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.CambioEstadoPeriodo,
                _usuarioActual.UsuarioId,
                empresa.Id,
                nameof(PeriodoCarga),
                periodo.Id,
                ahora,
                $"apertura={calendario.Clave}",
                _usuarioActual.DireccionIp),
            cancellationToken);

        await transaccion.ConfirmarAsync(cancellationToken);

        _logger.LogInformation(
            "Período {PeriodoId} ({Clave}) abierto para la empresa {EmpresaId}.",
            periodo.Id, calendario.Clave, empresa.Id);

        return MapeadorDePeriodos.ADto(periodo, empresa.RazonSocial, 0, 0);
    }
}
