namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lectura de archivos tabulares (CSV y XLSX) sin dependencias externas.
/// </summary>
/// <remarks>
/// La implementación vive en infraestructura. Se limita a la primera hoja y a
/// valores ya calculados: nunca evalúa fórmulas ni macros del archivo, que se
/// trata como datos no confiables.
/// </remarks>
public interface ILectorDeArchivosTabulares
{
    /// <summary>
    /// Lee un archivo.
    /// </summary>
    /// <param name="nombreArchivo">Nombre original, para detectar el formato por su extensión.</param>
    /// <param name="contenido">Bytes del archivo.</param>
    /// <param name="filaDeEncabezados">
    /// Número (base 1) de la fila de encabezados, o <c>null</c> para detectarla:
    /// la primera fila con al menos tres celdas de texto.
    /// </param>
    /// <param name="hojasPreferidas">
    /// En un libro de Excel, nombres de hoja que se buscan en orden (sin
    /// distinguir mayúsculas ni acentos); si ninguno existe se usa la primera
    /// hoja visible.
    /// </param>
    /// <returns>La tabla leída.</returns>
    /// <exception cref="Domain.Exceptions.DocumentoInvalidoException">
    /// Se lanza si el formato no es compatible o el archivo está corrupto.
    /// </exception>
    TablaLeida Leer(
        string nombreArchivo, ReadOnlyMemory<byte> contenido, int? filaDeEncabezados = null, IReadOnlyList<string>? hojasPreferidas = null);
}
