using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Periodos.Commands;

/// <summary>
/// Ejecuta <see cref="CambiarEstadoPeriodoCommand"/> y encola el aviso
/// correspondiente cuando el operador publica resultados.
/// </summary>
/// <remarks>
/// Sólo los roles transversales cambian el estado del ciclo: la empresa cliente
/// consume estados, no los produce. El dominio rechaza cualquier retroceso.
/// </remarks>
public sealed class CambiarEstadoPeriodoHandler : IManejadorDeComando<CambiarEstadoPeriodoCommand>
{
    private readonly IPeriodoRepository _periodos;
    private readonly IAuditoriaRepository _auditoria;
    private readonly INotificationService _notificaciones;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PoliticaDeAcceso _politicaDeAcceso;
    private readonly TimeProvider _reloj;
    private readonly ILogger<CambiarEstadoPeriodoHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CambiarEstadoPeriodoHandler"/>.
    /// </summary>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="notificaciones">Publicador de avisos.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public CambiarEstadoPeriodoHandler(
        IPeriodoRepository periodos,
        IAuditoriaRepository auditoria,
        INotificationService notificaciones,
        IUsuarioActual usuarioActual,
        IUnitOfWork unitOfWork,
        PoliticaDeAcceso politicaDeAcceso,
        TimeProvider reloj,
        ILogger<CambiarEstadoPeriodoHandler> logger)
    {
        _periodos = periodos;
        _auditoria = auditoria;
        _notificaciones = notificaciones;
        _usuarioActual = usuarioActual;
        _unitOfWork = unitOfWork;
        _politicaDeAcceso = politicaDeAcceso;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="comando"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no es transversal, si no tiene habilitada la gestión de
    /// períodos o si el período no existe para la empresa indicada.
    /// </exception>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza si la transición implica retroceder o el período ya está cerrado.
    /// </exception>
    public async Task EjecutarAsync(
        CambiarEstadoPeriodoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        if (!_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{_usuarioActual.Rol}' no puede cambiar el estado de un período.");
        }

        _politicaDeAcceso.GarantizarPuedeEjecutar(_usuarioActual.Rol, _usuarioActual.Permisos, AccionDelSistema.GestionarPeriodos);

        Guid empresaId = comando.EmpresaId
            ?? throw new AccesoNoAutorizadoException("Debe indicarse la empresa propietaria del período.");

        PeriodoCarga periodo =
            await _periodos.ObtenerPorIdAsync(comando.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException(
                $"El período '{comando.PeriodoId}' no existe o no pertenece a la empresa '{empresaId}'.");

        DateTimeOffset ahora = _reloj.GetUtcNow();
        EstadoPeriodo estadoAnterior = periodo.Estado;

        periodo.CambiarEstado(comando.NuevoEstado, ahora);

        await using (ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken))
        {
            await _periodos.ActualizarAsync(periodo, cancellationToken);

            await _auditoria.AgregarAsync(
                RegistroAuditoria.Exitoso(
                    AccionAuditada.CambioEstadoPeriodo,
                    _usuarioActual.UsuarioId,
                    empresaId,
                    nameof(PeriodoCarga),
                    periodo.Id,
                    ahora,
                    $"{estadoAnterior}->{comando.NuevoEstado}{FormatearComentario(comando.Comentario)}",
                    _usuarioActual.DireccionIp),
                cancellationToken);

            await transaccion.ConfirmarAsync(cancellationToken);
        }

        if (comando.NuevoEstado == EstadoPeriodo.ResultadosDisponibles)
        {
            await _notificaciones.EncolarAsync(
                new AvisoPendiente(
                    TipoDeAviso.ResultadosDisponibles,
                    empresaId,
                    periodo.Id,
                    periodo.Descripcion,
                    CantidadDocumentos: 0,
                    ahora),
                cancellationToken);
        }

        _logger.LogInformation(
            "Período {PeriodoId}: {EstadoAnterior} -> {EstadoNuevo}.",
            periodo.Id, estadoAnterior, comando.NuevoEstado);
    }

    /// <summary>Da formato a la nota opcional para el detalle de la bitácora.</summary>
    /// <param name="comentario">Nota escrita por el usuario.</param>
    /// <returns>El sufijo <c>; nota=…</c>, o cadena vacía si no hay nota.</returns>
    private static string FormatearComentario(string? comentario)
        => string.IsNullOrWhiteSpace(comentario) ? string.Empty : $"; nota={comentario.Trim()}";
}
