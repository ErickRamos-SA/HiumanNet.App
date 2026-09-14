namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Petición con la que el cliente avisa de que terminó de subir el archivo.
/// </summary>
/// <param name="HuellaSha256">
/// Hash SHA-256 del contenido subido, en hexadecimal. El servidor lo contrasta
/// con el que reporta Blob Storage para verificar la integridad.
/// </param>
/// <remarks>
/// Tras la confirmación el documento pasa a <c>Escaneando</c> y permanece no
/// descargable hasta que el antimalware emita su veredicto.
/// </remarks>
public sealed record ConfirmarCargaRequest(string HuellaSha256);
