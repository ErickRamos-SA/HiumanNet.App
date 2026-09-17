using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Documentos;

/// <summary>
/// Lee los archivos que el servidor procesa (importación de incidencias y
/// cotejo) a partir de los documentos intercambiados en el período.
/// </summary>
/// <remarks>
/// Todo archivo que alimenta el cálculo entra por el período: se sube con el
/// flujo de carga directa, supera el análisis antimalware y queda en la
/// bitácora. Esta clase es el único camino por el que un caso de uso lee su
/// contenido, y comprueba empresa, tipo y disponibilidad antes de hacerlo.
/// </remarks>
public sealed class ArchivosDelPeriodo
{
    private readonly IDocumentoRepository _documentos;
    private readonly IAlmacenDocumentos _almacen;
    private readonly PoliticaDeCarga _politica;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ArchivosDelPeriodo"/>.
    /// </summary>
    /// <param name="documentos">Repositorio de documentos.</param>
    /// <param name="almacen">Almacén de documentos.</param>
    /// <param name="politica">Política de carga, para el tamaño máximo.</param>
    public ArchivosDelPeriodo(IDocumentoRepository documentos, IAlmacenDocumentos almacen, PoliticaDeCarga politica)
    {
        _documentos = documentos;
        _almacen = almacen;
        _politica = politica;
    }

    /// <summary>
    /// Lee un archivo del período.
    /// </summary>
    /// <param name="documentoId">Documento solicitado.</param>
    /// <param name="empresaId">Empresa efectiva del solicitante.</param>
    /// <param name="tiposAdmitidos">Tipos de documento que acepta el caso de uso.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El documento y su contenido.</returns>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el documento no existe o es de otra empresa.</exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el tipo no se admite, si el archivo aún no está disponible o si excede el tamaño máximo.
    /// </exception>
    public async Task<ArchivoDelPeriodo> LeerAsync(
        Guid documentoId, Guid empresaId, IReadOnlyCollection<TipoDocumento> tiposAdmitidos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tiposAdmitidos);

        Documento documento = await _documentos.ObtenerPorIdAsync(documentoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El documento '{documentoId}' no existe o pertenece a otra empresa.");

        if (!tiposAdmitidos.Contains(documento.Tipo))
        {
            throw new DocumentoInvalidoException(
                $"El archivo «{documento.NombreOriginal}» es de tipo {documento.Tipo}; aquí sólo se admiten archivos de tipo {string.Join(" o ", tiposAdmitidos)}.");
        }

        documento.GarantizarQueEsDescargable();

        byte[] contenido = await _almacen.LeerAsync(documento.RutaBlob, _politica.TamanoMaximo.Bytes, cancellationToken);
        return new ArchivoDelPeriodo(documento, contenido);
    }
}
