namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Respuesta a una solicitud de carga: identifica el documento reservado y
/// entrega la URL SAS con la que el cliente sube el archivo directamente a
/// Blob Storage.
/// </summary>
/// <param name="DocumentoId">Identificador del documento reservado, en estado pendiente.</param>
/// <param name="UrlCarga">URL SAS de escritura, de un solo blob y corta vigencia.</param>
/// <param name="ExpiraEn">Instante en que caduca la URL, en UTC.</param>
/// <param name="EncabezadoTipoBlob">
/// Valor que el cliente debe enviar en el encabezado <c>x-ms-blob-type</c> al
/// hacer <c>PUT</c> contra la URL.
/// </param>
/// <remarks>
/// La URL es de mínimo privilegio: alcanza a un único blob, concede sólo
/// escritura y vive minutos (ARQUITECTURA.md §6.2). El servidor nunca recibe el
/// contenido del archivo.
/// </remarks>
public sealed record SolicitarCargaResponse(
    Guid DocumentoId,
    Uri UrlCarga,
    DateTimeOffset ExpiraEn,
    string EncabezadoTipoBlob = "BlockBlob");
