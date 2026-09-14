namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Confirma que el archivo terminó de subirse a Blob Storage.
/// </summary>
/// <param name="DocumentoId">Documento cuya carga se confirma.</param>
/// <param name="HuellaSha256">Hash SHA-256 hexadecimal del contenido subido.</param>
public sealed record ConfirmarCargaCommand(Guid DocumentoId, string HuellaSha256);
