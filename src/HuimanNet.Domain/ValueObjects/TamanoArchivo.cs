using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.ValueObjects;

/// <summary>
/// Representa el tamaño inmutable de un archivo, validado en su construcción.
/// </summary>
public readonly record struct TamanoArchivo
{
    /// <summary>Número de bytes que contiene un megabyte binario.</summary>
    public const long BytesPorMegabyte = 1_048_576L;

    private TamanoArchivo(long bytes) => Bytes = bytes;

    /// <summary>
    /// Obtiene el tamaño del archivo expresado en bytes.
    /// </summary>
    /// <value>Siempre mayor que cero; el límite superior lo define la política de carga.</value>
    public long Bytes { get; }

    /// <summary>
    /// Obtiene el tamaño expresado en megabytes, para presentación en la interfaz.
    /// </summary>
    /// <value>Valor derivado de <see cref="Bytes"/>, redondeado a dos decimales.</value>
    public decimal Megabytes => Math.Round(Bytes / (decimal)BytesPorMegabyte, 2);

    /// <summary>
    /// Crea un <see cref="TamanoArchivo"/> a partir de un número de bytes.
    /// </summary>
    /// <param name="bytes">Tamaño declarado por el cliente, en bytes.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si <paramref name="bytes"/> no es mayor que cero.
    /// </exception>
    public static TamanoArchivo DesdeBytes(long bytes)
    {
        if (bytes <= 0)
        {
            throw new DocumentoInvalidoException("El tamaño del archivo debe ser mayor que cero.");
        }

        return new TamanoArchivo(bytes);
    }

    /// <summary>
    /// Crea un <see cref="TamanoArchivo"/> a partir de un número de megabytes.
    /// </summary>
    /// <param name="megabytes">Tamaño en megabytes binarios.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el valor resultante en bytes no es mayor que cero.
    /// </exception>
    public static TamanoArchivo DesdeMegabytes(int megabytes)
        => DesdeBytes(megabytes * BytesPorMegabyte);

    /// <summary>
    /// Indica si este tamaño supera el límite indicado.
    /// </summary>
    /// <param name="limite">Tamaño máximo permitido por la política de carga.</param>
    /// <returns><c>true</c> si excede el límite; en caso contrario, <c>false</c>.</returns>
    public bool Excede(TamanoArchivo limite) => Bytes > limite.Bytes;

    /// <summary>
    /// Devuelve una representación legible del tamaño.
    /// </summary>
    /// <returns>Cadena con el tamaño en megabytes, por ejemplo <c>"3.25 MB"</c>.</returns>
    public override string ToString()
        => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Megabytes} MB");
}
