using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Documentos;

/// <summary>
/// Traduce entidades <see cref="Documento"/> a su contrato de transporte.
/// </summary>
/// <remarks>
/// Mapeo escrito a mano por requisito de compatibilidad con Native AOT: las
/// bibliotecas de mapeo por convención usan reflexión en tiempo de ejecución y
/// no sobreviven al recorte (ESPECIFICACION.md §2).
/// </remarks>
public static class MapeadorDeDocumentos
{
    /// <summary>
    /// Proyecta un documento a su DTO de transporte.
    /// </summary>
    /// <param name="documento">Entidad de origen.</param>
    /// <param name="nombreDeQuienCargo">Nombre del usuario que realizó la carga.</param>
    /// <returns>El DTO equivalente.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="documento"/> es <c>null</c>.
    /// </exception>
    public static DocumentoDto ADto(Documento documento, string nombreDeQuienCargo)
    {
        ArgumentNullException.ThrowIfNull(documento);

        return new DocumentoDto(
            documento.Id,
            documento.PeriodoId,
            documento.EmpresaId,
            documento.Tipo,
            documento.NombreOriginal.Valor,
            documento.Tamano.Bytes,
            documento.Estado,
            documento.FechaSolicitud,
            documento.FechaCargaConfirmada,
            nombreDeQuienCargo,
            documento.EsDescargable);
    }
}
