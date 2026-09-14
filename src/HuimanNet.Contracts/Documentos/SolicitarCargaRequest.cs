using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Petición para iniciar la carga de un documento y obtener una URL SAS de escritura.
/// </summary>
/// <param name="PeriodoId">Período al que se asociará el documento.</param>
/// <param name="Tipo">Tipo funcional del documento.</param>
/// <param name="NombreArchivo">Nombre original del archivo, con extensión.</param>
/// <param name="TamanoBytes">Tamaño declarado del archivo, en bytes.</param>
/// <param name="EmpresaId">
/// Empresa objetivo. Sólo la usan los roles transversales (operador y
/// administrador); para la empresa cliente se ignora y se impone la del token.
/// </param>
/// <remarks>
/// El servidor valida rol, empresa, extensión, tamaño y estado del período
/// <b>antes</b> de emitir el SAS. Un archivo no permitido nunca llega a Blob Storage.
/// </remarks>
public sealed record SolicitarCargaRequest(
    Guid PeriodoId,
    TipoDocumento Tipo,
    string NombreArchivo,
    long TamanoBytes,
    Guid? EmpresaId = null);
