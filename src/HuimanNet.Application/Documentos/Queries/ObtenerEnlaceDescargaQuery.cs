using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Documentos.Queries;

/// <summary>
/// Solicita el enlace temporal de descarga de un documento.
/// </summary>
/// <param name="DocumentoId">Documento que se desea descargar.</param>
public sealed record ObtenerEnlaceDescargaQuery(Guid DocumentoId);

/// <summary>
/// Ejecuta <see cref="ObtenerEnlaceDescargaQuery"/>: autoriza al solicitante,
/// comprueba que el documento superó el escaneo, emite la URL SAS de lectura y
/// deja constancia en la bitácora.
/// </summary>
/// <remarks>
/// Aunque nominalmente es una consulta, <b>escribe</b> un asiento de auditoría:
/// registrar quién descargó cada documento de nómina es un requisito de
/// cumplimiento, no una opción (ARQUITECTURA.md §6.3). Los intentos rechazados
/// también se auditan.
/// </remarks>
public sealed class ObtenerEnlaceDescargaHandler
    : IManejadorDeConsulta<ObtenerEnlaceDescargaQuery, EnlaceDescargaResponse>
{
    private readonly IDocumentoRepository _documentos;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ObtenerEnlaceDescargaHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ObtenerEnlaceDescargaHandler"/>.
    /// </summary>
    /// <param name="documentos">Repositorio de documentos.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="almacen">Almacén de documentos.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol y tipo.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ObtenerEnlaceDescargaHandler(
        IDocumentoRepository documentos,
        IAuditoriaRepository auditoria,
        IAlmacenDocumentos almacen,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso,
        TimeProvider reloj,
        ILogger<ObtenerEnlaceDescargaHandler> logger)
    {
        _documentos = documentos;
        _auditoria = auditoria;
        _almacen = almacen;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el documento no existe para el solicitante o su rol no puede descargarlo.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento aún no superó el escaneo de malware.
    /// </exception>
    public async Task<EnlaceDescargaResponse> EjecutarAsync(
        ObtenerEnlaceDescargaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        DateTimeOffset ahora = _reloj.GetUtcNow();

        // La empresa cliente puede tener varias empresas: el documento se busca
        // sin filtro y se descarta si pertenece a una que no es suya.
        Documento? documento = await _documentos.ObtenerPorIdSinFiltroDeEmpresaAsync(consulta.DocumentoId, cancellationToken);

        if (documento is not null
            && !_politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol)
            && !_usuarioActual.Empresas.Contains(documento.EmpresaId))
        {
            documento = null;
        }

        if (documento is null)
        {
            await AuditarRechazoAsync(consulta.DocumentoId, ahora, "documento inexistente o de otra empresa", cancellationToken);
            throw new AccesoNoAutorizadoException(
                $"El documento '{consulta.DocumentoId}' no existe o pertenece a otra empresa.");
        }

        try
        {
            _politicaDeAcceso.GarantizarPuedeDescargar(
                _usuarioActual.Rol, _usuarioActual.Empresas, documento);
        }
        catch (DomainException excepcion)
        {
            await AuditarRechazoAsync(documento.Id, ahora, excepcion.Codigo, cancellationToken);
            throw;
        }

        EnlaceTemporal enlace = await _almacen.CrearEnlaceDeLecturaAsync(
            documento.RutaBlob, documento.NombreOriginal.Valor, cancellationToken);

        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.Descarga,
                _usuarioActual.UsuarioId,
                documento.EmpresaId,
                nameof(Documento),
                documento.Id,
                ahora,
                $"tipo={documento.Tipo}",
                _usuarioActual.DireccionIp),
            cancellationToken);

        _logger.LogInformation(
            "Enlace de descarga emitido para el documento {DocumentoId}.", documento.Id);

        return new EnlaceDescargaResponse(
            documento.Id,
            enlace.Url,
            documento.NombreOriginal.Valor,
            documento.Tamano.Bytes,
            enlace.ExpiraEn);
    }

    private Task AuditarRechazoAsync(
        Guid documentoId, DateTimeOffset momento, string motivo, CancellationToken cancellationToken)
        => _auditoria.AgregarAsync(
            RegistroAuditoria.Fallido(
                AccionAuditada.AccesoDenegado,
                _usuarioActual.UsuarioId,
                _usuarioActual.EmpresaId,
                nameof(Documento),
                documentoId,
                momento,
                motivo,
                _usuarioActual.DireccionIp),
            cancellationToken);
}
