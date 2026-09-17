namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Abstracción del almacenamiento de documentos y de la emisión de enlaces
/// temporales de acceso directo.
/// </summary>
/// <remarks>
/// Los usuarios nunca suben ni descargan el contenido de los archivos a
/// través del servidor de aplicación: éste sólo autoriza y firma. El cliente
/// sube y descarga directo contra el almacenamiento, lo que evita que el
/// servidor sea cuello de botella de ancho de banda (ARQUITECTURA.md §3.3).
/// La única lectura del lado del servidor es <see cref="LeerAsync"/>, para
/// procesar archivos que ya están en un período (importación de incidencias y
/// cotejo).
/// <para>
/// La vigencia de las firmas es responsabilidad de la implementación: se
/// configura en infraestructura y se devuelve en <see cref="EnlaceTemporal.ExpiraEn"/>.
/// </para>
/// </remarks>
public interface IAlmacenDocumentos
{
    /// <summary>
    /// Emite un enlace de <b>sólo escritura</b> para subir un documento.
    /// </summary>
    /// <param name="rutaBlob">Ruta del blob generada por el sistema.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El enlace firmado y su instante de caducidad.</returns>
    /// <remarks>
    /// Mínimo privilegio: un solo blob, permiso exclusivo de escritura y
    /// vigencia de minutos.
    /// </remarks>
    Task<EnlaceTemporal> CrearEnlaceDeEscrituraAsync(
        string rutaBlob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emite un enlace de <b>sólo lectura</b> para descargar un documento.
    /// </summary>
    /// <param name="rutaBlob">Ruta del blob generada por el sistema.</param>
    /// <param name="nombreDescarga">
    /// Nombre original con el que debe guardarse el archivo en el equipo del usuario.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El enlace firmado y su instante de caducidad.</returns>
    Task<EnlaceTemporal> CrearEnlaceDeLecturaAsync(
        string rutaBlob, string nombreDescarga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las propiedades de un blob ya cargado.
    /// </summary>
    /// <param name="rutaBlob">Ruta del blob generada por el sistema.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las propiedades del blob, o <c>null</c> si el archivo no existe.</returns>
    /// <remarks>
    /// Permite verificar en el servidor el tamaño real subido en lugar de creer
    /// al cliente.
    /// </remarks>
    Task<PropiedadesDeBlob?> ObtenerPropiedadesAsync(
        string rutaBlob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Traslada un blob al contenedor de cuarentena.
    /// </summary>
    /// <param name="rutaBlob">Ruta del blob infectado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    /// <remarks>Se invoca cuando el escaneo de malware devuelve un veredicto positivo.</remarks>
    Task MoverACuarentenaAsync(string rutaBlob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lee el contenido de un documento para que el propio servidor lo procese.
    /// </summary>
    /// <param name="rutaBlob">Ruta del blob generada por el sistema.</param>
    /// <param name="bytesMaximos">Tamaño máximo aceptado, en bytes.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los bytes del archivo.</returns>
    /// <exception cref="HuimanNet.Domain.Exceptions.DocumentoInvalidoException">
    /// Se lanza si el archivo no existe en el almacenamiento o excede el máximo.
    /// </exception>
    /// <remarks>
    /// Lo usan la importación de incidencias y el cotejo, siempre sobre un
    /// documento del período que ya superó el análisis antimalware; nunca sobre
    /// un archivo enviado fuera del flujo de carga.
    /// </remarks>
    Task<byte[]> LeerAsync(string rutaBlob, long bytesMaximos, CancellationToken cancellationToken = default);
}
