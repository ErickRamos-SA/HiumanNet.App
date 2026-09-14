using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de documentos.
/// </summary>
/// <remarks>
/// El parámetro <c>empresaId</c> es obligatorio en las lecturas por identificador:
/// forma parte de la cláusula <c>WHERE</c> y es la barrera técnica que impide que
/// una empresa vea documentos de otra (ARQUITECTURA.md §6.2).
/// </remarks>
public interface IDocumentoRepository
{
    /// <summary>
    /// Obtiene un documento por su identificador, restringido a la empresa indicada.
    /// </summary>
    /// <param name="id">Identificador único del documento.</param>
    /// <param name="empresaId">
    /// Empresa del usuario solicitante. El filtro es obligatorio: impide que una
    /// empresa acceda a documentos de otra.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El documento encontrado, o <c>null</c> si no existe o no pertenece a la empresa.</returns>
    Task<Documento?> ObtenerPorIdAsync(
        Guid id, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un documento por su identificador sin filtrar por empresa.
    /// </summary>
    /// <param name="id">Identificador único del documento.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El documento encontrado, o <c>null</c> si no existe.</returns>
    /// <remarks>
    /// Reservado a operaciones transversales del operador de nómina, del
    /// administrador y del procesamiento de veredictos de escaneo. La autorización
    /// debe verificarse antes de invocarlo.
    /// </remarks>
    Task<Documento?> ObtenerPorIdSinFiltroDeEmpresaAsync(
        Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un documento a partir de la ruta de su blob.
    /// </summary>
    /// <param name="rutaBlob">Ruta generada por el sistema dentro del contenedor.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El documento encontrado, o <c>null</c> si la ruta no corresponde a ninguno.</returns>
    /// <remarks>Lo usa el procesamiento de veredictos de Defender for Storage.</remarks>
    Task<Documento?> ObtenerPorRutaBlobAsync(
        string rutaBlob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los documentos de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del usuario solicitante.</param>
    /// <param name="tipo">Tipo a filtrar, o <c>null</c> para devolver todos.</param>
    /// <param name="soloDescargables">
    /// Si es <c>true</c>, devuelve únicamente los documentos en estado
    /// <see cref="EstadoDocumento.Disponible"/>.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los documentos que satisfacen el filtro, del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<Documento>> ListarPorPeriodoAsync(
        Guid periodoId,
        Guid empresaId,
        TipoDocumento? tipo = null,
        bool soloDescargables = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta los documentos disponibles de un período por tipo.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="tipo">Tipo de documento a contar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Número de documentos en estado <see cref="EstadoDocumento.Disponible"/>.</returns>
    Task<int> ContarDisponiblesAsync(
        Guid periodoId, TipoDocumento tipo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un nuevo documento.
    /// </summary>
    /// <param name="documento">Documento a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarAsync(Documento documento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el estado y los metadatos de un documento existente.
    /// </summary>
    /// <param name="documento">Documento con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarAsync(Documento documento, CancellationToken cancellationToken = default);
}
