using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Ejecuta <see cref="ConfirmarCargaCommand"/>: verifica contra el
/// almacenamiento que el archivo existe, fija su tamaño real y lo deja a la
/// espera del escaneo de malware.
/// </summary>
/// <remarks>
/// El tamaño no se toma del cliente sino de las propiedades que reporta el
/// almacenamiento: un cliente podría declarar un tamaño y subir otro. El
/// documento queda en <see cref="EstadoDocumento.Escaneando"/> y sigue siendo
/// <b>no descargable</b> hasta que el antimalware emita su veredicto.
/// <para>
/// Cuando el analizador configurado devuelve un veredicto inmediato (entorno
/// local sin Defender), el veredicto se registra en el acto con el mismo caso
/// de uso que consume Event Grid en Azure: el flujo no cambia entre entornos.
/// </para>
/// </remarks>
public sealed class ConfirmarCargaHandler
    : IManejadorDeComando<ConfirmarCargaCommand, DocumentoDto>
{
    private readonly IDocumentoRepository _documentos;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IAnalizadorDeMalware _analizador;
    private readonly IManejadorDeComando<RegistrarResultadoEscaneoCommand> _registrarEscaneo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PoliticaDeAcceso _politicaDeAcceso;
    private readonly PoliticaDeCarga _politicaDeCarga;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ConfirmarCargaHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConfirmarCargaHandler"/>.
    /// </summary>
    /// <param name="documentos">Repositorio de documentos.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="almacen">Almacén de documentos.</param>
    /// <param name="analizador">Analizador antimalware.</param>
    /// <param name="registrarEscaneo">Caso de uso que registra el veredicto del escaneo.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol y tipo.</param>
    /// <param name="politicaDeCarga">Política de extensiones y tamaño máximo.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ConfirmarCargaHandler(
        IDocumentoRepository documentos,
        IAuditoriaRepository auditoria,
        IAlmacenDocumentos almacen,
        IAnalizadorDeMalware analizador,
        IManejadorDeComando<RegistrarResultadoEscaneoCommand> registrarEscaneo,
        IUsuarioActual usuarioActual,
        IUnitOfWork unitOfWork,
        PoliticaDeAcceso politicaDeAcceso,
        PoliticaDeCarga politicaDeCarga,
        TimeProvider reloj,
        ILogger<ConfirmarCargaHandler> logger)
    {
        _documentos = documentos;
        _auditoria = auditoria;
        _almacen = almacen;
        _analizador = analizador;
        _registrarEscaneo = registrarEscaneo;
        _usuarioActual = usuarioActual;
        _unitOfWork = unitOfWork;
        _politicaDeAcceso = politicaDeAcceso;
        _politicaDeCarga = politicaDeCarga;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="comando"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el documento no existe para el solicitante o si quien confirma
    /// no es quien solicitó la carga.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el archivo no está en el almacenamiento, si excede el tamaño
    /// máximo o si el documento no estaba pendiente.
    /// </exception>
    public async Task<DocumentoDto> EjecutarAsync(
        ConfirmarCargaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Documento documento = await ObtenerDocumentoAutorizadoAsync(comando.DocumentoId, cancellationToken);

        if (documento.CargadoPorUsuarioId != _usuarioActual.UsuarioId)
        {
            throw new AccesoNoAutorizadoException(
                $"El usuario '{_usuarioActual.UsuarioId}' intentó confirmar una carga iniciada por otro usuario.");
        }

        PropiedadesDeBlob propiedades =
            await _almacen.ObtenerPropiedadesAsync(documento.RutaBlob, cancellationToken)
            ?? throw new DocumentoInvalidoException(
                "El archivo no se encuentra en el almacenamiento. Vuelva a intentar la carga.");

        TamanoArchivo tamanoReal = TamanoArchivo.DesdeBytes(propiedades.TamanoBytes);

        if (tamanoReal.Excede(_politicaDeCarga.TamanoMaximo))
        {
            throw new DocumentoInvalidoException(
                $"El archivo subido pesa {tamanoReal} y el máximo permitido es {_politicaDeCarga.TamanoMaximo}.");
        }

        DateTimeOffset ahora = _reloj.GetUtcNow();
        documento.ConfirmarCarga(tamanoReal, HuellaArchivo.Crear(comando.HuellaSha256), ahora);

        await using (ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken))
        {
            await _documentos.ActualizarAsync(documento, cancellationToken);

            await _auditoria.AgregarAsync(
                RegistroAuditoria.Exitoso(
                    AccionAuditada.CargaConfirmada,
                    _usuarioActual.UsuarioId,
                    documento.EmpresaId,
                    nameof(Documento),
                    documento.Id,
                    ahora,
                    $"bytes={propiedades.TamanoBytes}",
                    _usuarioActual.DireccionIp),
                cancellationToken);

            await transaccion.ConfirmarAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Carga confirmada. Documento {DocumentoId} a la espera del escaneo de malware.",
            documento.Id);

        VeredictoDeEscaneo? veredicto = await _analizador.AnalizarAsync(documento.RutaBlob, cancellationToken);

        if (veredicto is not null)
        {
            await _registrarEscaneo.EjecutarAsync(
                new RegistrarResultadoEscaneoCommand(documento.RutaBlob, veredicto.EsLimpio, veredicto.Motivo),
                cancellationToken);

            documento = await _documentos.ObtenerPorIdSinFiltroDeEmpresaAsync(documento.Id, cancellationToken) ?? documento;
        }

        return MapeadorDeDocumentos.ADto(documento, _usuarioActual.NombreCompleto);
    }

    private async Task<Documento> ObtenerDocumentoAutorizadoAsync(
        Guid documentoId, CancellationToken cancellationToken)
    {
        Documento? documento = await _documentos.ObtenerPorIdSinFiltroDeEmpresaAsync(documentoId, cancellationToken);

        // La empresa cliente sólo confirma documentos de alguna de sus empresas.
        bool autorizado = documento is not null
            && (_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol)
                || _usuarioActual.Empresas.Contains(documento.EmpresaId));

        return autorizado
            ? documento!
            : throw new AccesoNoAutorizadoException($"El documento '{documentoId}' no existe o pertenece a otra empresa.");
    }
}
