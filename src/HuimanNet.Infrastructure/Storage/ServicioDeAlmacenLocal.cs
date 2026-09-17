using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HuimanNet.Contracts;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Storage;

/// <summary>
/// Núcleo del almacenamiento en disco local: rutas seguras, firma y
/// verificación de enlaces, escritura y lectura de archivos.
/// </summary>
/// <remarks>
/// Reproduce el modelo de Blob Storage con SAS: el servidor emite un enlace con
/// permiso único (lectura <i>o</i> escritura), un solo archivo y caducidad de
/// minutos, firmado con HMAC-SHA256. Los anfitriones (web y API) exponen el
/// punto de entrada <see cref="RutasApi.AlmacenLocal"/> que valida la firma y
/// sirve o recibe el archivo. Así el flujo de carga directa y confirmación es
/// idéntico en local y en Azure.
/// </remarks>
public sealed class ServicioDeAlmacenLocal
{
    /// <summary>Permiso de escritura del enlace.</summary>
    public const string PermisoEscritura = "w";

    /// <summary>Permiso de lectura del enlace.</summary>
    public const string PermisoLectura = "r";

    /// <summary>Longitud máxima de una ruta de blob.</summary>
    public const int LongitudMaximaRuta = 400;

    private readonly OpcionesDeAlmacenamiento _opciones;
    private readonly TimeProvider _reloj;
    private readonly byte[] _clave;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ServicioDeAlmacenLocal"/>
    /// y crea las carpetas que falten.
    /// </summary>
    /// <param name="opciones">Opciones de almacenamiento.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public ServicioDeAlmacenLocal(IOptions<OpcionesDeAlmacenamiento> opciones, TimeProvider reloj)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        _opciones = opciones.Value;
        _reloj = reloj;
        _clave = string.IsNullOrWhiteSpace(_opciones.ClaveDeFirmaLocal)
            ? RandomNumberGenerator.GetBytes(32)
            : SHA256.HashData(Encoding.UTF8.GetBytes(_opciones.ClaveDeFirmaLocal));

        string raiz = string.IsNullOrWhiteSpace(_opciones.RutaLocal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HuimanNet", "almacen")
            : Environment.ExpandEnvironmentVariables(_opciones.RutaLocal);

        Raiz = Path.GetFullPath(raiz);
        Directory.CreateDirectory(Path.Combine(Raiz, _opciones.ContenedorDocumentos));
        Directory.CreateDirectory(Path.Combine(Raiz, _opciones.ContenedorCuarentena));
    }

    /// <summary>Obtiene la carpeta raíz del almacenamiento.</summary>
    /// <value>Ruta absoluta.</value>
    public string Raiz { get; }

    /// <summary>Obtiene el nombre de la carpeta de documentos.</summary>
    /// <value>Tomado de <see cref="OpcionesDeAlmacenamiento.ContenedorDocumentos"/>.</value>
    public string ContenedorDocumentos => _opciones.ContenedorDocumentos;

    /// <summary>
    /// Emite un enlace firmado.
    /// </summary>
    /// <param name="permiso"><see cref="PermisoEscritura"/> o <see cref="PermisoLectura"/>.</param>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    /// <param name="expira">Instante de caducidad.</param>
    /// <param name="nombreDescarga">Nombre con el que se descargará el archivo (sólo lectura).</param>
    /// <returns>El enlace, absoluto si hay <see cref="OpcionesDeAlmacenamiento.UrlPublicaLocal"/>; relativo en otro caso.</returns>
    public Uri CrearUrl(string permiso, string rutaBlob, DateTimeOffset expira, string? nombreDescarga)
    {
        ValidarRuta(rutaBlob);

        long vence = expira.ToUnixTimeSeconds();
        string nombre = nombreDescarga ?? string.Empty;
        string firma = Firmar(permiso, rutaBlob, vence, nombre);

        string url = string.Create(
            CultureInfo.InvariantCulture,
            $"{_opciones.UrlPublicaLocal.TrimEnd('/')}{RutasApi.AlmacenLocal}/{rutaBlob}?p={permiso}&e={vence}&n={Uri.EscapeDataString(nombre)}&s={firma}");

        return new Uri(url, UriKind.RelativeOrAbsolute);
    }

    /// <summary>
    /// Comprueba la firma y la vigencia de un enlace.
    /// </summary>
    /// <param name="rutaBlob">Ruta solicitada.</param>
    /// <param name="permisoRequerido">Permiso que exige la operación.</param>
    /// <param name="permiso">Valor del parámetro <c>p</c>.</param>
    /// <param name="expira">Valor del parámetro <c>e</c>.</param>
    /// <param name="nombre">Valor del parámetro <c>n</c>.</param>
    /// <param name="firma">Valor del parámetro <c>s</c>.</param>
    /// <returns><c>true</c> si el enlace es auténtico, vigente y concede el permiso requerido.</returns>
    public bool Validar(string rutaBlob, string permisoRequerido, string? permiso, string? expira, string? nombre, string? firma)
    {
        if (permiso != permisoRequerido || string.IsNullOrEmpty(firma) || !EsRutaValida(rutaBlob)
            || !long.TryParse(expira, NumberStyles.None, CultureInfo.InvariantCulture, out long vence))
        {
            return false;
        }

        DateTimeOffset limite = DateTimeOffset.FromUnixTimeSeconds(vence);

        if (_reloj.GetUtcNow() > limite.AddMinutes(_opciones.MinutosToleranciaDeReloj))
        {
            return false;
        }

        byte[] esperada = Encoding.ASCII.GetBytes(Firmar(permiso, rutaBlob, vence, nombre ?? string.Empty));
        byte[] recibida = Encoding.ASCII.GetBytes(firma);

        return CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }

    /// <summary>
    /// Guarda un archivo de forma atómica (escritura a temporal y renombrado).
    /// </summary>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    /// <param name="contenido">Flujo con el contenido.</param>
    /// <param name="bytesMaximos">Tamaño máximo aceptado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El número de bytes escritos.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si el contenido excede el máximo.</exception>
    public async Task<long> GuardarAsync(string rutaBlob, Stream contenido, long bytesMaximos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        string destino = RutaFisica(_opciones.ContenedorDocumentos, rutaBlob);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        string temporal = destino + "." + Guid.NewGuid().ToString("N") + ".tmp";
        long total = 0;

        try
        {
            await using (var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
            {
                byte[] buffer = new byte[81_920];
                int leidos;

                while ((leidos = await contenido.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += leidos;

                    if (total > bytesMaximos)
                    {
                        throw new DocumentoInvalidoException("El archivo excede el tamaño máximo permitido.");
                    }

                    await archivo.WriteAsync(buffer.AsMemory(0, leidos), cancellationToken);
                }
            }

            File.Move(temporal, destino, overwrite: true);
            return total;
        }
        finally
        {
            if (File.Exists(temporal))
            {
                File.Delete(temporal);
            }
        }
    }

    /// <summary>
    /// Obtiene la información de un archivo de documentos.
    /// </summary>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    /// <returns>El archivo, o <c>null</c> si no existe.</returns>
    public FileInfo? ObtenerArchivo(string rutaBlob)
    {
        var info = new FileInfo(RutaFisica(_opciones.ContenedorDocumentos, rutaBlob));
        return info.Exists ? info : null;
    }

    /// <summary>
    /// Lee completo un archivo de documentos.
    /// </summary>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    /// <param name="bytesMaximos">Tamaño máximo aceptado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los bytes del archivo.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si el archivo no existe o excede el máximo.</exception>
    public async Task<byte[]> LeerAsync(string rutaBlob, long bytesMaximos, CancellationToken cancellationToken)
    {
        FileInfo archivo = ObtenerArchivo(rutaBlob)
            ?? throw new DocumentoInvalidoException("El archivo no se encuentra en el almacenamiento.");

        if (archivo.Length > bytesMaximos)
        {
            throw new DocumentoInvalidoException("El archivo excede el tamaño máximo permitido.");
        }

        return await File.ReadAllBytesAsync(archivo.FullName, cancellationToken);
    }

    /// <summary>
    /// Traslada un archivo a la carpeta de cuarentena.
    /// </summary>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    public void MoverACuarentena(string rutaBlob)
    {
        string origen = RutaFisica(_opciones.ContenedorDocumentos, rutaBlob);

        if (!File.Exists(origen))
        {
            return;
        }

        string destino = RutaFisica(_opciones.ContenedorCuarentena, rutaBlob);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        File.Move(origen, destino, overwrite: true);
    }

    /// <summary>
    /// Calcula la ruta física de un blob, garantizando que no escape de la raíz.
    /// </summary>
    /// <param name="contenedor">Carpeta del contenedor.</param>
    /// <param name="rutaBlob">Ruta generada por el sistema.</param>
    /// <returns>Ruta absoluta dentro de la raíz.</returns>
    /// <exception cref="DocumentoInvalidoException">Se lanza si la ruta no es válida.</exception>
    public string RutaFisica(string contenedor, string rutaBlob)
    {
        ValidarRuta(rutaBlob);

        string baseContenedor = Path.GetFullPath(Path.Combine(Raiz, contenedor)) + Path.DirectorySeparatorChar;
        string completa = Path.GetFullPath(Path.Combine(baseContenedor, rutaBlob.Replace('/', Path.DirectorySeparatorChar)));

        return completa.StartsWith(baseContenedor, StringComparison.OrdinalIgnoreCase)
            ? completa
            : throw new DocumentoInvalidoException("La ruta del documento no es válida.");
    }

    /// <summary>
    /// Indica si una ruta tiene la forma de las rutas que genera el sistema.
    /// </summary>
    /// <param name="rutaBlob">Ruta a comprobar.</param>
    /// <returns><c>true</c> si sólo contiene minúsculas, dígitos, <c>/</c>, <c>.</c>, <c>-</c> y <c>_</c>, sin <c>..</c>.</returns>
    public static bool EsRutaValida(string? rutaBlob)
    {
        if (string.IsNullOrEmpty(rutaBlob) || rutaBlob.Length > LongitudMaximaRuta
            || rutaBlob.StartsWith('/') || rutaBlob.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char c in rutaBlob)
        {
            if (!(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '/' or '.' or '-' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Exige que la ruta sea una de las que genera el sistema.</summary>
    /// <param name="rutaBlob">Ruta del archivo.</param>
    /// <exception cref="DocumentoInvalidoException">Se lanza si la ruta tiene caracteres no permitidos.</exception>
    private static void ValidarRuta(string rutaBlob)
    {
        if (!EsRutaValida(rutaBlob))
        {
            throw new DocumentoInvalidoException("La ruta del documento no es válida.");
        }
    }

    /// <summary>Calcula la firma HMAC-SHA256 de una URL del almacén local.</summary>
    /// <param name="permiso">Permiso concedido (lectura o escritura).</param>
    /// <param name="rutaBlob">Ruta del archivo.</param>
    /// <param name="vence">Expiración, en segundos Unix.</param>
    /// <param name="nombre">Nombre de descarga.</param>
    /// <returns>La firma en Base64 URL.</returns>
    private string Firmar(string permiso, string rutaBlob, long vence, string nombre)
    {
        string datos = string.Create(CultureInfo.InvariantCulture, $"{permiso}\n{rutaBlob}\n{vence}\n{nombre}");
        return Base64Url.EncodeToString(HMACSHA256.HashData(_clave, Encoding.UTF8.GetBytes(datos)));
    }
}
