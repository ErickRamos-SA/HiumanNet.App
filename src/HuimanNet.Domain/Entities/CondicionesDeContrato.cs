using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Condiciones económicas de un contrato.
/// </summary>
/// <param name="SueldoPeriodoReal">Sueldo real pactado por período de pago.</param>
/// <param name="SalarioDiarioFiscal">Salario diario registrado ante el IMSS. Para un contrato IMSS puro coincide con el sueldo real entre los días del período.</param>
/// <param name="SalarioDiarioIntegrado">Salario diario integrado registrado ante el IMSS.</param>
/// <param name="Zona">Zona de salario mínimo del centro de trabajo.</param>
/// <param name="Infonavit">Crédito INFONAVIT, o <see cref="CreditoInfonavit.Ninguno"/>.</param>
/// <param name="FonacotMensual">Importe mensual del crédito FONACOT; cero si no hay.</param>
/// <param name="PensionAlimenticiaImporte">Importe fijo de pensión alimenticia por período.</param>
/// <param name="PensionAlimenticiaPorcentaje">Porcentaje de pensión alimenticia sobre percepciones, como fracción.</param>
/// <param name="PrestamoPersonalFijo">Descuento fijo de préstamo personal por período.</param>
/// <param name="BonoFijo">Bono fijo por período.</param>
/// <param name="HonorariosAplicaIva">Si el contrato por honorarios traslada IVA.</param>
/// <param name="PagaComplementoSindical">
/// Sueldo mixto IMSS + sindicato: si es <c>true</c>, la diferencia entre el
/// sueldo real y el neto fiscal se entrega vía sindicato o cooperativa, como en
/// el modelo de referencia. En un contrato IMSS puro debe ser <c>false</c>.
/// </param>
public sealed record CondicionesDeContrato(
    decimal SueldoPeriodoReal,
    decimal SalarioDiarioFiscal,
    decimal SalarioDiarioIntegrado,
    ZonaSalarioMinimo Zona,
    CreditoInfonavit Infonavit,
    decimal FonacotMensual,
    decimal PensionAlimenticiaImporte,
    decimal PensionAlimenticiaPorcentaje,
    decimal PrestamoPersonalFijo,
    decimal BonoFijo,
    bool HonorariosAplicaIva,
    bool PagaComplementoSindical);
