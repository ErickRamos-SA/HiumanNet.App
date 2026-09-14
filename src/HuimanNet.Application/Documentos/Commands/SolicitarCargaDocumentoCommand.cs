using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Solicita el permiso para cargar un documento y reserva su ruta en el
/// almacenamiento.
/// </summary>
/// <param name="PeriodoId">Período al que se asociará el documento.</param>
/// <param name="Tipo">Tipo funcional del documento.</param>
/// <param name="NombreArchivo">Nombre original del archivo, con extensión.</param>
/// <param name="TamanoBytes">Tamaño declarado del archivo, en bytes.</param>
/// <param name="EmpresaId">
/// Empresa objetivo. Sólo la aportan los roles transversales; para la empresa
/// cliente se impone la del token.
/// </param>
/// <remarks>
/// Este comando cubre también la publicación de resultados y ajustes por parte
/// del operador de nómina: qué combinaciones de rol y tipo son legales lo decide
/// <see cref="Domain.Services.PoliticaDeAcceso"/>, de modo que no hace falta un
/// caso de uso separado que duplicaría las reglas.
/// </remarks>
public sealed record SolicitarCargaDocumentoCommand(
    Guid PeriodoId,
    TipoDocumento Tipo,
    string NombreArchivo,
    long TamanoBytes,
    Guid? EmpresaId);
