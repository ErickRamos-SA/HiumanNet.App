using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Domain.Services;

/// <summary>
/// Política vigente de carga de archivos: qué extensiones se aceptan y cuál es
/// el tamaño máximo permitido.
/// </summary>
/// <remarks>
/// Es una lista blanca deliberada: cualquier extensión no enumerada se rechaza.
/// La validación ocurre <b>antes</b> de emitir la URL SAS de escritura, de modo
/// que un archivo no permitido nunca llega a Blob Storage.
/// </remarks>
public sealed class PoliticaDeCarga
{
    private readonly HashSet<string> _extensionesPermitidas;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PoliticaDeCarga"/>.
    /// </summary>
    /// <param name="extensionesPermitidas">
    /// Extensiones aceptadas, con punto inicial y sin distinguir mayúsculas.
    /// </param>
    /// <param name="tamanoMaximo">Tamaño máximo permitido por archivo.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="extensionesPermitidas"/> es <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Se lanza si la lista de extensiones está vacía.
    /// </exception>
    public PoliticaDeCarga(IEnumerable<string> extensionesPermitidas, TamanoArchivo tamanoMaximo)
    {
        ArgumentNullException.ThrowIfNull(extensionesPermitidas);

        _extensionesPermitidas = extensionesPermitidas
            .Where(static e => !string.IsNullOrWhiteSpace(e))
            .Select(static e => e.Trim().ToLowerInvariant())
            .Select(static e => e.StartsWith('.') ? e : "." + e)
            .ToHashSet(StringComparer.Ordinal);

        if (_extensionesPermitidas.Count == 0)
        {
            throw new ArgumentException(
                "La política de carga debe permitir al menos una extensión.",
                nameof(extensionesPermitidas));
        }

        TamanoMaximo = tamanoMaximo;
    }

    /// <summary>
    /// Obtiene el tamaño máximo permitido por archivo.
    /// </summary>
    /// <value>Límite superior aplicado antes de emitir el SAS de escritura.</value>
    public TamanoArchivo TamanoMaximo { get; }

    /// <summary>
    /// Obtiene las extensiones aceptadas.
    /// </summary>
    /// <value>Conjunto de sólo lectura, en minúsculas y con punto inicial.</value>
    public IReadOnlyCollection<string> ExtensionesPermitidas => _extensionesPermitidas;

    /// <summary>
    /// Obtiene la política predeterminada del portal.
    /// </summary>
    /// <value>Ofimática de nómina (Excel, CSV, PDF, texto y ZIP) con tope de 50 MB.</value>
    /// <remarks>
    /// Sirve como valor de respaldo si la configuración no define una política;
    /// consulte <c>OpcionesDeCarga</c> en la capa de infraestructura.
    /// </remarks>
    public static PoliticaDeCarga Predeterminada { get; } = new(
        [".xlsx", ".xls", ".csv", ".pdf", ".txt", ".zip"],
        TamanoArchivo.DesdeMegabytes(50));

    /// <summary>
    /// Indica si una extensión está permitida por la política.
    /// </summary>
    /// <param name="extension">Extensión con punto inicial, en cualquier caja.</param>
    /// <returns><c>true</c> si la extensión está en la lista blanca.</returns>
    public bool PermiteExtension(string? extension)
        => !string.IsNullOrWhiteSpace(extension)
           && _extensionesPermitidas.Contains(extension.Trim().ToLowerInvariant());
}
