using System.Globalization;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.ValueObjects;

/// <summary>
/// Huella criptográfica de un archivo, usada para verificar la integridad de la
/// carga y para detectar duplicados dentro de un mismo período.
/// </summary>
/// <remarks>
/// El cliente calcula la huella antes de subir y la envía al confirmar la carga;
/// el servidor la contrasta con la que reporta Blob Storage.
/// </remarks>
public readonly record struct HuellaArchivo
{
    /// <summary>Algoritmo de hash empleado por el sistema.</summary>
    public const string AlgoritmoPredeterminado = "SHA-256";

    /// <summary>Longitud en caracteres hexadecimales de un hash SHA-256.</summary>
    public const int LongitudSha256Hex = 64;

    private readonly string? _valorHex;

    private HuellaArchivo(string valorHex) => _valorHex = valorHex;

    /// <summary>
    /// Obtiene el hash en hexadecimal, en minúsculas y sin separadores.
    /// </summary>
    /// <value>64 caracteres para SHA-256. Cadena vacía si la instancia es <c>default</c>.</value>
    public string ValorHex => _valorHex ?? string.Empty;

    /// <summary>
    /// Obtiene el nombre del algoritmo que produjo la huella.
    /// </summary>
    /// <value>Siempre <see cref="AlgoritmoPredeterminado"/> en la versión 1.</value>
    public static string Algoritmo => AlgoritmoPredeterminado;

    /// <summary>
    /// Indica si la instancia contiene un valor.
    /// </summary>
    /// <value><c>true</c> si no hay huella calculada; en caso contrario, <c>false</c>.</value>
    public bool EstaVacia => string.IsNullOrEmpty(_valorHex);

    /// <summary>
    /// Crea una <see cref="HuellaArchivo"/> a partir de un hash SHA-256 hexadecimal.
    /// </summary>
    /// <param name="valorHex">Hash en hexadecimal, con o sin mayúsculas.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el valor no tiene la longitud de un SHA-256 o contiene
    /// caracteres que no son hexadecimales.
    /// </exception>
    public static HuellaArchivo Crear(string? valorHex)
    {
        if (string.IsNullOrWhiteSpace(valorHex))
        {
            throw new DocumentoInvalidoException("La huella del archivo es obligatoria.");
        }

        string normalizado = valorHex.Trim().ToLowerInvariant();

        if (normalizado.Length != LongitudSha256Hex)
        {
            throw new DocumentoInvalidoException(
                $"La huella debe ser un {AlgoritmoPredeterminado} de {LongitudSha256Hex} caracteres hexadecimales.");
        }

        foreach (char caracter in normalizado)
        {
            if (!char.IsAsciiHexDigitLower(caracter))
            {
                throw new DocumentoInvalidoException("La huella contiene caracteres no hexadecimales.");
            }
        }

        return new HuellaArchivo(normalizado);
    }

    /// <summary>
    /// Crea una <see cref="HuellaArchivo"/> a partir de los bytes de un hash.
    /// </summary>
    /// <param name="hash">Bytes devueltos por el algoritmo de hash.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si <paramref name="hash"/> no corresponde a un SHA-256.
    /// </exception>
    public static HuellaArchivo DesdeBytes(ReadOnlySpan<byte> hash)
        => Crear(Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture));

    /// <summary>
    /// Devuelve la huella en formato <c>algoritmo:valor</c>.
    /// </summary>
    /// <returns>Por ejemplo <c>"SHA-256:9f86d0..."</c>.</returns>
    public override string ToString() => $"{AlgoritmoPredeterminado}:{ValorHex}";
}
