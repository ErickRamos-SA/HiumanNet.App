using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Periodos;

/// <summary>
/// Traduce entidades <see cref="PeriodoCarga"/> a su contrato de transporte.
/// </summary>
/// <remarks>Mapeo manual por requisito de compatibilidad con Native AOT.</remarks>
public static class MapeadorDePeriodos
{
    /// <summary>
    /// Proyecta un período a su DTO de transporte.
    /// </summary>
    /// <param name="periodo">Entidad de origen.</param>
    /// <param name="razonSocialEmpresa">Razón social de la empresa propietaria.</param>
    /// <param name="documentosDelCliente">Documentos disponibles aportados por el cliente.</param>
    /// <param name="documentosDeResultado">Documentos de resultado y ajuste disponibles.</param>
    /// <returns>El DTO equivalente.</returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="periodo"/> es <c>null</c>.
    /// </exception>
    public static PeriodoDto ADto(
        PeriodoCarga periodo,
        string razonSocialEmpresa,
        int documentosDelCliente,
        int documentosDeResultado)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        return new PeriodoDto(
            periodo.Id,
            periodo.EmpresaId,
            razonSocialEmpresa,
            periodo.Calendario.Clave,
            periodo.Descripcion,
            periodo.Estado,
            periodo.FechaApertura,
            periodo.FechaLimiteCarga,
            periodo.FechaCierre,
            documentosDelCliente,
            documentosDeResultado);
    }
}
