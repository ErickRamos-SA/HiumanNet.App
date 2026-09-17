using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Crédito INFONAVIT vigente de un trabajador.
/// </summary>
/// <param name="Tipo">Modalidad del aviso de retención.</param>
/// <param name="Valor">Importe mensual, factor VSM o porcentaje (como fracción), según la modalidad.</param>
/// <param name="SeguroDeVivienda">Seguro de vivienda bimestral.</param>
public sealed record CreditoInfonavit(TipoDeCreditoInfonavit Tipo, decimal Valor, decimal SeguroDeVivienda)
{
    /// <summary>Obtiene la instancia que representa la ausencia de crédito.</summary>
    /// <value>Tipo <see cref="TipoDeCreditoInfonavit.Ninguno"/> con importes en cero.</value>
    public static CreditoInfonavit Ninguno { get; } = new(TipoDeCreditoInfonavit.Ninguno, 0m, 0m);
}
