using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Infrastructure.Archivos;

/// <summary>
/// Lee archivos CSV y XLSX/XLSM sin dependencias externas.
/// </summary>
/// <remarks>
/// El XLSX es un ZIP con XML: se lee con <see cref="ZipArchive"/> y
/// <see cref="XmlReader"/> en modo de sólo avance, sin cargar el documento
/// completo en memoria y sin reflexión (compatible con Native AOT). Se leen los
/// valores almacenados en cada celda —el resultado ya calculado de las
/// fórmulas—; las fórmulas y macros del archivo nunca se ejecutan, porque el
/// archivo es un dato no confiable.
/// </remarks>
public sealed class LectorDeArchivosTabulares : ILectorDeArchivosTabulares
{
    /// <summary>Número de filas iniciales en las que se busca la fila de encabezados.</summary>
    public const int FilasDeBusquedaDeEncabezado = 30;

    /// <summary>Número máximo de filas que se leen de un archivo.</summary>
    public const int FilasMaximas = 200_000;

    private static readonly XmlReaderSettings ConfiguracionXml = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    /// <inheritdoc/>
    public TablaLeida Leer(
        string nombreArchivo, ReadOnlyMemory<byte> contenido, int? filaDeEncabezados = null, IReadOnlyList<string>? hojasPreferidas = null)
    {
        if (contenido.IsEmpty)
        {
            throw new DocumentoInvalidoException("El archivo está vacío.");
        }

        string extension = Path.GetExtension(nombreArchivo ?? string.Empty).ToLowerInvariant();

        List<string?[]> filas = extension switch
        {
            ".csv" or ".txt" => LeerCsv(contenido),
            ".xlsx" or ".xlsm" => LeerXlsx(contenido, hojasPreferidas),
            _ => throw new DocumentoInvalidoException("Formato no compatible: use un archivo CSV o XLSX."),
        };

        return ConstruirTabla(filas, filaDeEncabezados);
    }

    /// <summary>
    /// Separa los encabezados de los datos y da a todas las filas el ancho de
    /// los encabezados.
    /// </summary>
    /// <param name="filas">Filas leídas del archivo.</param>
    /// <param name="filaDeEncabezados">Número de fila (desde 1) de los encabezados; se detecta si es <c>null</c>.</param>
    /// <returns>La tabla, con el número de fila del primer dato para los mensajes de error.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si el archivo no tiene filas.</exception>
    private static TablaLeida ConstruirTabla(List<string?[]> filas, int? filaDeEncabezados)
    {
        if (filas.Count == 0)
        {
            throw new DocumentoInvalidoException("El archivo no contiene filas.");
        }

        int indice = filaDeEncabezados is { } fila ? Math.Clamp(fila - 1, 0, filas.Count - 1) : DetectarEncabezado(filas);
        string?[] encabezados = filas[indice];
        int columnas = Math.Max(encabezados.Length, 1);

        var datos = new List<string?[]>(Math.Max(0, filas.Count - indice - 1));

        for (int i = indice + 1; i < filas.Count; i++)
        {
            string?[] original = filas[i];
            var normalizada = new string?[columnas];
            Array.Copy(original, normalizada, Math.Min(original.Length, columnas));
            datos.Add(normalizada);
        }

        return new TablaLeida([.. encabezados.Select(static e => e ?? string.Empty)], datos, indice + 2);
    }

    /// <summary>
    /// Elige como encabezado la fila con más celdas de texto (no numéricas)
    /// entre las primeras filas: las hojas de nómina suelen tener títulos y
    /// notas antes de la fila de columnas.
    /// </summary>
    /// <param name="filas">Filas leídas del archivo.</param>
    /// <returns>El índice, base cero, de la fila de encabezados.</returns>
    private static int DetectarEncabezado(List<string?[]> filas)
    {
        int mejor = 0;
        int mejorCuenta = -1;

        for (int i = 0; i < Math.Min(filas.Count, FilasDeBusquedaDeEncabezado); i++)
        {
            int cuenta = 0;

            foreach (string? celda in filas[i])
            {
                if (!string.IsNullOrWhiteSpace(celda)
                    && !decimal.TryParse(celda, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    cuenta++;
                }
            }

            if (cuenta > mejorCuenta)
            {
                mejor = i;
                mejorCuenta = cuenta;
            }
        }

        return mejor;
    }

    // ---------------------------------------------------------------- CSV ----

    /// <summary>
    /// Lee un CSV en UTF-8 (o Latin-1 si no es UTF-8 válido), con comillas
    /// dobles y el separador que se detecte en la primera línea.
    /// </summary>
    /// <param name="contenido">Bytes del archivo.</param>
    /// <returns>Las filas, hasta <see cref="FilasMaximas"/>.</returns>
    private static List<string?[]> LeerCsv(ReadOnlyMemory<byte> contenido)
    {
        string texto;

        try
        {
            texto = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(contenido.Span);
        }
        catch (DecoderFallbackException)
        {
            // Archivos exportados por Excel en Windows con la página de códigos occidental.
            texto = Encoding.Latin1.GetString(contenido.Span);
        }

        if (texto.Length > 0 && texto[0] == '﻿')
        {
            texto = texto[1..];
        }

        char separador = DetectarSeparador(texto);
        var filas = new List<string?[]>();
        var fila = new List<string?>();
        var celda = new StringBuilder();
        bool entreComillas = false;

        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];

            if (entreComillas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        celda.Append('"');
                        i++;
                    }
                    else
                    {
                        entreComillas = false;
                    }
                }
                else
                {
                    celda.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                entreComillas = true;
            }
            else if (c == separador)
            {
                fila.Add(celda.ToString());
                celda.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n')
                {
                    i++;
                }

                fila.Add(celda.ToString());
                celda.Clear();
                filas.Add([.. fila]);
                fila.Clear();

                if (filas.Count >= FilasMaximas)
                {
                    break;
                }
            }
            else
            {
                celda.Append(c);
            }
        }

        if (celda.Length > 0 || fila.Count > 0)
        {
            fila.Add(celda.ToString());
            filas.Add([.. fila]);
        }

        return filas;
    }

    /// <summary>Elige el separador más frecuente en la primera línea.</summary>
    /// <param name="texto">Contenido del CSV.</param>
    /// <returns>Tabulador, punto y coma o coma; coma si hay empate.</returns>
    private static char DetectarSeparador(string texto)
    {
        int fin = texto.IndexOf('\n', StringComparison.Ordinal);
        ReadOnlySpan<char> primera = fin < 0 ? texto : texto.AsSpan(0, fin);

        int comas = primera.Count(',');
        int puntoYComa = primera.Count(';');
        int tabuladores = primera.Count('\t');

        return tabuladores > comas && tabuladores > puntoYComa ? '\t' : puntoYComa > comas ? ';' : ',';
    }

    // --------------------------------------------------------------- XLSX ----

    /// <summary>Lee la hoja elegida de un libro XLSX sin dependencias externas.</summary>
    /// <param name="contenido">Bytes del archivo.</param>
    /// <param name="hojasPreferidas">Nombres de hoja a buscar en orden; si ninguno existe, la primera visible.</param>
    /// <returns>Las filas de la hoja, hasta <see cref="FilasMaximas"/>.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si el libro está dañado o su XML no es válido.</exception>
    private static List<string?[]> LeerXlsx(ReadOnlyMemory<byte> contenido, IReadOnlyList<string>? hojasPreferidas)
    {
        try
        {
            using var flujo = new MemoryStream(contenido.ToArray(), writable: false);
            using var zip = new ZipArchive(flujo, ZipArchiveMode.Read);

            List<string> compartidas = LeerCadenasCompartidas(zip);
            string rutaHoja = ElegirHoja(zip, hojasPreferidas);

            ZipArchiveEntry entrada = zip.GetEntry(rutaHoja)
                ?? throw new DocumentoInvalidoException("El libro no contiene la hoja indicada en su índice.");

            using Stream hoja = entrada.Open();
            return LeerHoja(hoja, compartidas);
        }
        catch (InvalidDataException)
        {
            throw new DocumentoInvalidoException("El archivo XLSX está dañado o no es un libro de Excel.");
        }
        catch (XmlException)
        {
            throw new DocumentoInvalidoException("El archivo XLSX tiene un contenido XML no válido.");
        }
    }

    /// <summary>
    /// Lee la tabla de cadenas compartidas del libro, a la que remiten las
    /// celdas de texto; omite las guías fonéticas.
    /// </summary>
    /// <param name="zip">Libro abierto.</param>
    /// <returns>Las cadenas por índice; vacía si el libro no tiene tabla.</returns>
    private static List<string> LeerCadenasCompartidas(ZipArchive zip)
    {
        var cadenas = new List<string>();
        ZipArchiveEntry? entrada = zip.GetEntry("xl/sharedStrings.xml");

        if (entrada is null)
        {
            return cadenas;
        }

        using Stream flujo = entrada.Open();
        using XmlReader xml = XmlReader.Create(flujo, ConfiguracionXml);
        var actual = new StringBuilder();
        bool dentroDeSi = false;

        while (xml.Read())
        {
            if (xml.NodeType == XmlNodeType.Element)
            {
                switch (xml.LocalName)
                {
                    case "si":
                        dentroDeSi = true;
                        actual.Clear();

                        if (xml.IsEmptyElement)
                        {
                            cadenas.Add(string.Empty);
                            dentroDeSi = false;
                        }

                        break;
                    case "rPh":
                        // Guías fonéticas (japonés): no forman parte del texto visible.
                        xml.Skip();
                        break;
                    case "t" when dentroDeSi:
                        actual.Append(xml.ReadElementContentAsString());
                        break;
                }
            }
            else if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "si")
            {
                cadenas.Add(actual.ToString());
                dentroDeSi = false;
            }
        }

        return cadenas;
    }

    /// <summary>
    /// Elige la hoja a leer: la primera de las preferidas que exista, si no la
    /// primera visible y, en último caso, la primera del libro.
    /// </summary>
    /// <param name="zip">Libro abierto.</param>
    /// <param name="hojasPreferidas">Nombres de hoja a buscar en orden.</param>
    /// <returns>La ruta de la hoja dentro del paquete.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si el paquete no tiene índice de libro.</exception>
    private static string ElegirHoja(ZipArchive zip, IReadOnlyList<string>? hojasPreferidas)
    {
        var relaciones = new Dictionary<string, string>(StringComparer.Ordinal);
        ZipArchiveEntry? rels = zip.GetEntry("xl/_rels/workbook.xml.rels");

        if (rels is not null)
        {
            using Stream flujo = rels.Open();
            using XmlReader xml = XmlReader.Create(flujo, ConfiguracionXml);

            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "Relationship")
                {
                    string? id = xml.GetAttribute("Id");
                    string? destino = xml.GetAttribute("Target");

                    if (id is not null && destino is not null)
                    {
                        relaciones[id] = destino.StartsWith('/') ? destino.TrimStart('/') : "xl/" + destino;
                    }
                }
            }
        }

        var hojas = new List<(string Nombre, string Ruta, bool Visible)>();
        ZipArchiveEntry libro = zip.GetEntry("xl/workbook.xml")
            ?? throw new DocumentoInvalidoException("El archivo no es un libro de Excel válido.");

        using (Stream flujo = libro.Open())
        using (XmlReader xml = XmlReader.Create(flujo, ConfiguracionXml))
        {
            while (xml.Read())
            {
                if (xml.NodeType != XmlNodeType.Element || xml.LocalName != "sheet")
                {
                    continue;
                }

                string nombre = xml.GetAttribute("name") ?? string.Empty;
                string? id = xml.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                string estado = xml.GetAttribute("state") ?? "visible";

                if (id is not null && relaciones.TryGetValue(id, out string? ruta))
                {
                    hojas.Add((nombre, ruta, estado == "visible"));
                }
            }
        }

        if (hojas.Count == 0)
        {
            return "xl/worksheets/sheet1.xml";
        }

        foreach (string preferida in hojasPreferidas ?? [])
        {
            string buscada = TablaLeida.Normalizar(preferida);

            foreach ((string nombre, string ruta, bool _) in hojas)
            {
                if (TablaLeida.Normalizar(nombre) == buscada)
                {
                    return ruta;
                }
            }
        }

        foreach ((string _, string ruta, bool visible) in hojas)
        {
            if (visible)
            {
                return ruta;
            }
        }

        return hojas[0].Ruta;
    }

    /// <summary>
    /// Lee las filas de una hoja; rellena las filas vacías para que los números
    /// coincidan con los de Excel.
    /// </summary>
    /// <param name="hoja">XML de la hoja.</param>
    /// <param name="compartidas">Cadenas compartidas del libro.</param>
    /// <returns>Las filas, hasta <see cref="FilasMaximas"/>.</returns>
    private static List<string?[]> LeerHoja(Stream hoja, List<string> compartidas)
    {
        var filas = new List<string?[]>();
        using XmlReader xml = XmlReader.Create(hoja, ConfiguracionXml);

        var celdas = new List<string?>();
        int filaActual = 0;

        while (xml.Read())
        {
            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "row")
            {
                int numero = int.TryParse(xml.GetAttribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out int r)
                    ? r
                    : filaActual + 1;

                // Las filas vacías no aparecen en el XML: se rellenan para que los
                // números de fila de los mensajes de error coincidan con Excel.
                while (filas.Count < numero - 1)
                {
                    filas.Add([]);
                }

                filaActual = numero;
                celdas.Clear();

                if (xml.IsEmptyElement)
                {
                    filas.Add([]);
                    continue;
                }

                LeerFila(xml, celdas, compartidas);
                filas.Add([.. celdas]);

                if (filas.Count >= FilasMaximas)
                {
                    break;
                }
            }
        }

        return filas;
    }

    /// <summary>
    /// Lee las celdas de la fila actual en su columna; las omitidas por Excel
    /// quedan en <c>null</c>.
    /// </summary>
    /// <param name="xml">Lector situado en el elemento <c>row</c>.</param>
    /// <param name="celdas">Lista que se llena con los valores de la fila.</param>
    /// <param name="compartidas">Cadenas compartidas del libro.</param>
    private static void LeerFila(XmlReader xml, List<string?> celdas, List<string> compartidas)
    {
        int profundidad = xml.Depth;

        while (xml.Read() && !(xml.NodeType == XmlNodeType.EndElement && xml.Depth == profundidad))
        {
            if (xml.NodeType != XmlNodeType.Element || xml.LocalName != "c")
            {
                continue;
            }

            int columna = IndiceDeColumna(xml.GetAttribute("r"), celdas.Count);
            string? tipo = xml.GetAttribute("t");
            string? valor = xml.IsEmptyElement ? null : LeerValorDeCelda(xml, tipo, compartidas);

            while (celdas.Count < columna)
            {
                celdas.Add(null);
            }

            if (celdas.Count == columna)
            {
                celdas.Add(valor);
            }
            else
            {
                celdas[columna] = valor;
            }
        }
    }

    /// <summary>Lee el valor de la celda actual según su tipo.</summary>
    /// <param name="xml">Lector situado en el elemento <c>c</c>.</param>
    /// <param name="tipo">Atributo <c>t</c> de la celda: cadena compartida, en línea, booleano o error.</param>
    /// <param name="compartidas">Cadenas compartidas del libro.</param>
    /// <returns>El texto de la celda; <c>null</c> si está vacía o tiene un error.</returns>
    private static string? LeerValorDeCelda(XmlReader xml, string? tipo, List<string> compartidas)
    {
        int profundidad = xml.Depth;
        string? valor = null;
        var enLinea = new StringBuilder();

        while (xml.Read() && !(xml.NodeType == XmlNodeType.EndElement && xml.Depth == profundidad))
        {
            if (xml.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (xml.LocalName == "v")
            {
                valor = xml.ReadElementContentAsString();

                if (xml.NodeType == XmlNodeType.EndElement && xml.Depth == profundidad)
                {
                    break;
                }
            }
            else if (xml.LocalName == "t" && tipo == "inlineStr")
            {
                enLinea.Append(xml.ReadElementContentAsString());
            }
        }

        return tipo switch
        {
            "s" => int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out int indice)
                   && indice >= 0 && indice < compartidas.Count
                ? compartidas[indice]
                : null,
            "inlineStr" => enLinea.ToString(),
            "b" => valor == "1" ? "1" : "0",
            "e" => null,
            _ => valor,
        };
    }

    /// <summary>
    /// Convierte una referencia de celda (<c>AB12</c>) en índice de columna base cero.
    /// </summary>
    /// <param name="referencia">Atributo <c>r</c> de la celda.</param>
    /// <param name="siguiente">Columna que corresponde si la celda no trae referencia.</param>
    /// <returns>El índice de la columna.</returns>
    private static int IndiceDeColumna(string? referencia, int siguiente)
    {
        if (string.IsNullOrEmpty(referencia))
        {
            return siguiente;
        }

        int columna = 0;

        foreach (char c in referencia)
        {
            if (c is >= 'A' and <= 'Z')
            {
                columna = (columna * 26) + (c - 'A' + 1);
            }
            else
            {
                break;
            }
        }

        return columna == 0 ? siguiente : Math.Min(columna - 1, 16_383);
    }
}
