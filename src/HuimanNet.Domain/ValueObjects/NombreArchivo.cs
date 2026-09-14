using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.ValueObjects;

/// <summary>
/// Nombre de archivo aportado por el usuario, saneado y validado en su construcción.
/// </summary>
/// <remarks>
/// Este objeto de valor <b>nunca</b> define la ruta en Blob Storage: el nombre de
/// blob lo genera el sistema con un GUID para evitar <i>path traversal</i> y
/// colisiones (ARQUITECTURA.md §6.1). El nombre original sólo se conserva como
/// metadato para mostrarlo al usuario y para nombrar la descarga.
/// </remarks>
public readonly record struct NombreArchivo
{
    /// <summary>Longitud máxima aceptada para el nombre original.</summary>
    public const int LongitudMaxima = 255;

    private readonly string? _valor;
    private readonly string? _extension;

    private NombreArchivo(string valor, string extension)
    {
        _valor = valor;
        _extension = extension;
    }

    /// <summary>
    /// Obtiene el nombre de archivo saneado, sin componentes de ruta.
    /// </summary>
    /// <value>Por ejemplo <c>"incidencias-agosto.xlsx"</c>. Cadena vacía si la instancia es <c>default</c>.</value>
    public string Valor => _valor ?? string.Empty;

    /// <summary>
    /// Obtiene la extensión en minúsculas, incluido el punto inicial.
    /// </summary>
    /// <value>Por ejemplo <c>".xlsx"</c>. Cadena vacía si la instancia es <c>default</c>.</value>
    public string Extension => _extension ?? string.Empty;

    /// <summary>
    /// Crea un <see cref="NombreArchivo"/> a partir del texto aportado por el usuario.
    /// </summary>
    /// <param name="valor">Nombre original tal y como lo envía el cliente.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el nombre está vacío, excede <see cref="LongitudMaxima"/>,
    /// contiene separadores de ruta o caracteres no válidos, o carece de extensión.
    /// </exception>
    public static NombreArchivo Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DocumentoInvalidoException("El nombre del archivo es obligatorio.");
        }

        string limpio = valor.Trim();

        if (limpio.Length > LongitudMaxima)
        {
            throw new DocumentoInvalidoException(
                $"El nombre del archivo excede {LongitudMaxima} caracteres.");
        }

        if (limpio.Contains('/', StringComparison.Ordinal)
            || limpio.Contains('\\', StringComparison.Ordinal)
            || limpio.Contains("..", StringComparison.Ordinal))
        {
            throw new DocumentoInvalidoException(
                "El nombre del archivo no puede contener rutas ni secuencias de directorio.");
        }

        if (limpio.AsSpan().IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new DocumentoInvalidoException(
                "El nombre del archivo contiene caracteres no válidos.");
        }

        string extension = Path.GetExtension(limpio).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || extension.Length < 2)
        {
            throw new DocumentoInvalidoException(
                "El nombre del archivo debe incluir una extensión.");
        }

        return new NombreArchivo(limpio, extension);
    }

    /// <summary>
    /// Devuelve el nombre de archivo saneado.
    /// </summary>
    /// <returns>El valor de <see cref="Valor"/>.</returns>
    public override string ToString() => Valor;
}
