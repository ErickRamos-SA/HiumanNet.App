using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Ejecuta <see cref="RegistrarResultadoEscaneoCommand"/>: publica el documento
/// si el escaneo fue limpio o lo pone en cuarentena si detectó malware.
/// </summary>
/// <remarks>
/// Es el punto donde un archivo pasa a ser descargable. Antes de este veredicto
/// el documento existe en el almacenamiento pero es inaccesible para todos los
/// roles (ARQUITECTURA.md §6.1).
/// <para>
/// El aviso por correo se encola aquí y no al confirmar la carga: notificar
/// antes del escaneo invitaría a descargar algo aún no verificado.
/// </para>
/// </remarks>
public sealed class RegistrarResultadoEscaneoHandler
    : IManejadorDeComando<RegistrarResultadoEscaneoCommand>
{
    private readonly IDocumentoRepository _documentos;
    private readonly IPeriodoRepository _periodos;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAlmacenDocumentos _almacen;
    private readonly INotificationService _notificaciones;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;
    private readonly ILogger<RegistrarResultadoEscaneoHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RegistrarResultadoEscaneoHandler"/>.
    /// </summary>
    /// <param name="documentos">Repositorio de documentos.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="almacen">Almacén de documentos.</param>
    /// <param name="notificaciones">Publicador de avisos.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public RegistrarResultadoEscaneoHandler(
        IDocumentoRepository documentos,
        IPeriodoRepository periodos,
        IAuditoriaRepository auditoria,
        IAlmacenDocumentos almacen,
        INotificationService notificaciones,
        IUnitOfWork unitOfWork,
        TimeProvider reloj,
        ILogger<RegistrarResultadoEscaneoHandler> logger)
    {
        _documentos = documentos;
        _periodos = periodos;
        _auditoria = auditoria;
        _almacen = almacen;
        _notificaciones = notificaciones;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="comando"/> es <c>null</c>.</exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si la ruta no corresponde a ningún documento registrado.
    /// </exception>
    public async Task EjecutarAsync(
        RegistrarResultadoEscaneoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Documento documento =
            await _documentos.ObtenerPorRutaBlobAsync(comando.RutaBlob, cancellationToken)
            ?? throw new DocumentoInvalidoException(
                $"No hay ningún documento registrado con la ruta '{comando.RutaBlob}'.");

        DateTimeOffset ahora = _reloj.GetUtcNow();
        AvisoPendiente? aviso;

        if (comando.EsLimpio)
        {
            documento.MarcarDisponible(ahora);
            aviso = await PrepararAvisoAsync(documento, ahora, cancellationToken);
        }
        else
        {
            documento.MarcarEnCuarentena(
                comando.Motivo ?? "El análisis antimalware detectó contenido malicioso.", ahora);

            await _almacen.MoverACuarentenaAsync(documento.RutaBlob, cancellationToken);

            aviso = new AvisoPendiente(
                TipoDeAviso.ArchivoEnCuarentena,
                documento.EmpresaId,
                documento.PeriodoId,
                DescripcionPeriodo: string.Empty,
                CantidadDocumentos: 1,
                ahora);
        }

        await using (ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken))
        {
            await _documentos.ActualizarAsync(documento, cancellationToken);

            if (comando.EsLimpio && documento.EsAportadoPorCliente)
            {
                await MarcarRecepcionDelPeriodoAsync(documento, cancellationToken);
            }

            await _auditoria.AgregarAsync(
                RegistroAuditoria.Exitoso(
                    AccionAuditada.ResultadoDeEscaneo,
                    IdentidadesDelSistema.Sistema,
                    documento.EmpresaId,
                    nameof(Documento),
                    documento.Id,
                    ahora,
                    comando.EsLimpio ? "veredicto=limpio" : "veredicto=malicioso"),
                cancellationToken);

            await transaccion.ConfirmarAsync(cancellationToken);
        }

        if (aviso is not null)
        {
            await _notificaciones.EncolarAsync(aviso, cancellationToken);
        }

        if (comando.EsLimpio)
        {
            _logger.LogInformation("Documento {DocumentoId} disponible tras superar el escaneo.", documento.Id);
        }
        else
        {
            _logger.LogWarning(
                "Documento {DocumentoId} en cuarentena: el escaneo detectó contenido malicioso.",
                documento.Id);
        }
    }

    private async Task<AvisoPendiente?> PrepararAvisoAsync(
        Documento documento, DateTimeOffset ahora, CancellationToken cancellationToken)
    {
        PeriodoCarga? periodo =
            await _periodos.ObtenerPorIdAsync(documento.PeriodoId, documento.EmpresaId, cancellationToken);

        if (periodo is null)
        {
            return null;
        }

        TipoDeAviso tipo = documento.EsAportadoPorCliente
            ? TipoDeAviso.DocumentosRecibidos
            : TipoDeAviso.ResultadosDisponibles;

        return new AvisoPendiente(
            tipo,
            documento.EmpresaId,
            documento.PeriodoId,
            periodo.Descripcion,
            CantidadDocumentos: 1,
            ahora);
    }

    private async Task MarcarRecepcionDelPeriodoAsync(
        Documento documento, CancellationToken cancellationToken)
    {
        PeriodoCarga? periodo =
            await _periodos.ObtenerPorIdAsync(documento.PeriodoId, documento.EmpresaId, cancellationToken);

        if (periodo is null || periodo.Estado != EstadoPeriodo.Abierto)
        {
            return;
        }

        periodo.MarcarRecepcion();
        await _periodos.ActualizarAsync(periodo, cancellationToken);
    }
}
