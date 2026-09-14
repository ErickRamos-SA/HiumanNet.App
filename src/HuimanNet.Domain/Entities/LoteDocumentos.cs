namespace HuimanNet.Domain.Entities;

/// <summary>
/// Agrupa los documentos que un usuario carga en una misma operación, para que
/// la notificación al destinatario sea una sola y no una por archivo.
/// </summary>
/// <remarks>
/// El lote no es propietario de los documentos: sólo referencia sus
/// identificadores. La raíz de agregado sigue siendo <see cref="Documento"/>.
/// </remarks>
public sealed class LoteDocumentos
{
    private readonly List<Guid> _documentoIds;

    private LoteDocumentos(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        Guid creadoPorUsuarioId,
        DateTimeOffset fechaCreacion,
        string? comentario,
        IEnumerable<Guid> documentoIds)
    {
        Id = id;
        EmpresaId = empresaId;
        PeriodoId = periodoId;
        CreadoPorUsuarioId = creadoPorUsuarioId;
        FechaCreacion = fechaCreacion;
        Comentario = comentario;
        _documentoIds = [.. documentoIds];
    }

    /// <summary>
    /// Obtiene el identificador único del lote.
    /// </summary>
    /// <value>Clave primaria del lote.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene la empresa propietaria del lote.
    /// </summary>
    /// <value>Coincide siempre con la empresa de los documentos que agrupa.</value>
    public Guid EmpresaId { get; }

    /// <summary>
    /// Obtiene el período al que pertenece el lote.
    /// </summary>
    /// <value>Identificador del <see cref="PeriodoCarga"/> asociado.</value>
    public Guid PeriodoId { get; }

    /// <summary>
    /// Obtiene el usuario que creó el lote.
    /// </summary>
    /// <value>Identificador local del <see cref="Usuario"/> responsable.</value>
    public Guid CreadoPorUsuarioId { get; }

    /// <summary>
    /// Obtiene el instante de creación del lote.
    /// </summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaCreacion { get; }

    /// <summary>
    /// Obtiene el comentario opcional que acompaña al envío.
    /// </summary>
    /// <value>Texto libre del usuario, o <c>null</c>. Nunca debe contener datos personales.</value>
    public string? Comentario { get; }

    /// <summary>
    /// Obtiene los documentos que integran el lote.
    /// </summary>
    /// <value>Lista de sólo lectura con los identificadores de <see cref="Documento"/>.</value>
    public IReadOnlyList<Guid> DocumentoIds => _documentoIds;

    /// <summary>
    /// Crea un lote vacío para una operación de carga.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="periodoId">Período asociado.</param>
    /// <param name="creadoPorUsuarioId">Usuario que inicia la carga.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <param name="comentario">Comentario opcional del remitente.</param>
    /// <returns>El lote recién creado, sin documentos.</returns>
    public static LoteDocumentos Crear(
        Guid empresaId,
        Guid periodoId,
        Guid creadoPorUsuarioId,
        DateTimeOffset momento,
        string? comentario = null)
        => new(
            Guid.CreateVersion7(),
            empresaId,
            periodoId,
            creadoPorUsuarioId,
            momento,
            string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(),
            []);

    /// <summary>
    /// Reconstruye un lote a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="periodoId">Período asociado.</param>
    /// <param name="creadoPorUsuarioId">Usuario que creó el lote.</param>
    /// <param name="fechaCreacion">Instante de creación en UTC.</param>
    /// <param name="comentario">Comentario del remitente, si existe.</param>
    /// <param name="documentoIds">Identificadores de los documentos agrupados.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static LoteDocumentos Rehidratar(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        Guid creadoPorUsuarioId,
        DateTimeOffset fechaCreacion,
        string? comentario,
        IEnumerable<Guid> documentoIds)
        => new(id, empresaId, periodoId, creadoPorUsuarioId, fechaCreacion, comentario, documentoIds);

    /// <summary>
    /// Añade un documento al lote.
    /// </summary>
    /// <param name="documento">Documento a agrupar.</param>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="documento"/> es <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si el documento pertenece a otra empresa o a otro período.
    /// </exception>
    public void Agregar(Documento documento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        if (documento.EmpresaId != EmpresaId || documento.PeriodoId != PeriodoId)
        {
            throw new InvalidOperationException(
                "El documento no corresponde a la empresa y período del lote.");
        }

        if (!_documentoIds.Contains(documento.Id))
        {
            _documentoIds.Add(documento.Id);
        }
    }
}
