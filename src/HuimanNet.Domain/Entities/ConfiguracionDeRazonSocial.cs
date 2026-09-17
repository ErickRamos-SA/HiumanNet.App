using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Configuración de operación de una razón social: banderas que gobiernan
/// el cálculo de todos sus trabajadores.
/// </summary>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="SubsidioAbsorbido">Si la empresa absorbe el subsidio al empleo en el complemento sindical.</param>
/// <param name="AplicaFaltasProporcionales">Si las faltas se castigan con el factor del séptimo día.</param>
/// <param name="ModalidadDeComision">Base de la comisión al cliente.</param>
/// <param name="PorcentajeComision">Porcentaje de comisión, como fracción.</param>
/// <param name="ZonaIsn">Regla para la tasa del ISN.</param>
/// <param name="TasaIva">
/// Tasa de IVA de la factura, como fracción (cero si no aplica); <c>null</c>
/// para usar la tasa general del catálogo (<see cref="Nomina.ClavesDeParametro.TasaIva"/>).
/// </param>
/// <param name="PorcentajeOtrosCostos">Porcentaje de otros costos sobre el neto pagado, como fracción.</param>
/// <param name="PrimaDeRiesgo">Prima de riesgo de trabajo, como fracción; <c>null</c> para usar el parámetro general.</param>
public sealed record ConfiguracionDeRazonSocial(
    TipoDeServicio TipoDeServicio,
    bool SubsidioAbsorbido,
    bool AplicaFaltasProporcionales,
    ModalidadDeComision ModalidadDeComision,
    decimal PorcentajeComision,
    ZonaIsn ZonaIsn,
    decimal? TasaIva,
    decimal PorcentajeOtrosCostos,
    decimal? PrimaDeRiesgo);
