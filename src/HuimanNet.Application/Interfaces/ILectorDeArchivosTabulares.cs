using System.Globalization;
using System.Text;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Contenido tabular leído de un archivo CSV o XLSX: una fila de encabezados y
/// las filas de datos.
/// </summary>
public sealed class TablaLeida
{
    private readonly Dictionary<string, int> _indice;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TablaLeida"/>.
    /// </summary>
    /// <param name="encabezados">Encabezados tal y como aparecen en el archivo.</param>
    /// <param name="filas">Filas de datos; cada celda puede ser <c>null</c>.</param>
    /// <param name="primeraFilaDeDatos">Número (base 1) de la primera fila de datos en el archivo, para los mensajes de error.</param>
    public TablaLeida(IReadOnlyList<string> encabezados, IReadOnlyList<string?[]> filas, int primeraFilaDeDatos)
    {
        ArgumentNullException.ThrowIfNull(encabezados);
        ArgumentNullException.ThrowIfNull(filas);

        Encabezados = encabezados;
        Filas = filas;
        PrimeraFilaDeDatos = primeraFilaDeDatos;
        _indice = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < encabezados.Count; i++)
        {
            string clave = Normalizar(encabezados[i]);

            if (clave.Length > 0)
            {
                _indice.TryAdd(clave, i);
            }
        }
    }

    /// <summary>Obtiene los encabezados originales.</summary>
    /// <value>Lista de sólo lectura.</value>
    public IReadOnlyList<string> Encabezados { get; }

    /// <summary>Obtiene las filas de datos.</summary>
    /// <value>Lista de sólo lectura; cada fila tiene tantas celdas como encabezados.</value>
    public IReadOnlyList<string?[]> Filas { get; }

    /// <summary>Obtiene el número de fila del archivo donde empiezan los datos.</summary>
    /// <value>Base 1.</value>
    public int PrimeraFilaDeDatos { get; }

    /// <summary>
    /// Busca la columna cuyo encabezado, normalizado, coincide con alguno de los nombres indicados.
    /// </summary>
    /// <param name="nombres">Nombres candidatos, en cualquier caja y con o sin acentos.</param>
    /// <returns>Índice de la columna, o <c>-1</c> si ninguno coincide.</returns>
    public int IndiceDe(params string[] nombres)
    {
        foreach (string nombre in nombres)
        {
            if (_indice.TryGetValue(Normalizar(nombre), out int indice))
            {
                return indice;
            }
        }

        return -1;
    }

    /// <summary>
    /// Lee una celda como texto.
    /// </summary>
    /// <param name="fila">Fila de datos.</param>
    /// <param name="indice">Índice de columna, o <c>-1</c>.</param>
    /// <returns>El texto recortado, o <c>null</c> si la columna no existe o la celda está vacía.</returns>
    public static string? Texto(string?[] fila, int indice)
    {
        ArgumentNullException.ThrowIfNull(fila);

        if (indice < 0 || indice >= fila.Length)
        {
            return null;
        }

        string? valor = fila[indice]?.Trim();
        return string.IsNullOrEmpty(valor) ? null : valor;
    }

    /// <summary>
    /// Lee una celda como número decimal.
    /// </summary>
    /// <param name="fila">Fila de datos.</param>
    /// <param name="indice">Índice de columna, o <c>-1</c>.</param>
    /// <param name="valor">Número leído.</param>
    /// <returns><c>true</c> si la celda tenía un número; <c>false</c> si estaba vacía o la columna no existe.</returns>
    /// <exception cref="FormatException">Se lanza si la celda tiene texto que no es un número.</exception>
    public static bool Numero(string?[] fila, int indice, out decimal valor)
    {
        string? texto = Texto(fila, indice);

        if (texto is null)
        {
            valor = 0m;
            return false;
        }

        string limpio = texto.Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (limpio.EndsWith('%'))
        {
            limpio = limpio[..^1];
            valor = decimal.Parse(limpio, NumberStyles.Float, CultureInfo.InvariantCulture) / 100m;
            return true;
        }

        valor = decimal.Parse(limpio, NumberStyles.Float, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>
    /// Normaliza un encabezado: mayúsculas, sin acentos, sin espacios ni signos.
    /// </summary>
    /// <param name="texto">Encabezado original.</param>
    /// <returns>Clave comparable.</returns>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);

        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                resultado.Append(char.ToUpperInvariant(c));
            }
        }

        return resultado.ToString();
    }
}

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
