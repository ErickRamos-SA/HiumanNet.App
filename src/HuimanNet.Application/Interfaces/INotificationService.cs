namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Motivo por el que se genera un aviso a los usuarios.
/// </summary>
public enum TipoDeAviso
{
    /// <summary>Valor no especificado. Nunca debe encolarse.</summary>
    NoEspecificado = 0,

    /// <summary>La empresa cliente publicó documentos: se avisa al operador de nómina.</summary>
    DocumentosRecibidos = 1,

    /// <summary>El operador publicó resultados o ajustes: se avisa a la empresa cliente.</summary>
    ResultadosDisponibles = 2,

    /// <summary>El escaneo detectó malware: se avisa al administrador y a quien cargó el archivo.</summary>
    ArchivoEnCuarentena = 3,
}

/// <summary>
/// Aviso pendiente de envío, tal y como viaja por la cola.
/// </summary>
/// <param name="Tipo">Motivo del aviso.</param>
/// <param name="EmpresaId">Empresa a la que se refiere el aviso.</param>
/// <param name="PeriodoId">Período al que se refiere el aviso.</param>
/// <param name="DescripcionPeriodo">Descripción legible del período, para el cuerpo del correo.</param>
/// <param name="CantidadDocumentos">Número de documentos involucrados.</param>
/// <param name="Momento">Instante en que se generó el aviso, en UTC.</param>
/// <remarks>
/// El mensaje contiene únicamente identificadores y contadores: <b>nunca</b>
/// datos personales ni nombres de archivo, que podrían filtrarse en registros
/// de la cola (ESPECIFICACION.md §9, seguridad).
/// </remarks>
public sealed record AvisoPendiente(
    TipoDeAviso Tipo,
    Guid EmpresaId,
    Guid PeriodoId,
    string DescripcionPeriodo,
    int CantidadDocumentos,
    DateTimeOffset Momento);

/// <summary>
/// Publicación de avisos hacia el canal de notificación.
/// </summary>
/// <remarks>
/// La petición web sólo <b>encola</b>: el envío del correo lo realiza un
/// <c>BackgroundService</c> aparte, de modo que un fallo del proveedor de correo
/// nunca hace fracasar una carga de documentos (ARQUITECTURA.md §4.1).
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Encola un aviso para su envío asíncrono.
    /// </summary>
    /// <param name="aviso">Aviso a publicar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EncolarAsync(AvisoPendiente aviso, CancellationToken cancellationToken = default);
}
