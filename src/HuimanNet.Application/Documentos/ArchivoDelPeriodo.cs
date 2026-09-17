using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Documentos;

/// <summary>
/// Archivo de un período leído del almacenamiento.
/// </summary>
/// <param name="Documento">Documento del período.</param>
/// <param name="Contenido">Bytes del archivo.</param>
public sealed record ArchivoDelPeriodo(Documento Documento, byte[] Contenido);
