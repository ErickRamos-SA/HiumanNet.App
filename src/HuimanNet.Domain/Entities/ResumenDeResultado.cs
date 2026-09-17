using HuimanNet.Domain.Nomina;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Cifras principales del resultado de un contrato, leídas de los conceptos
/// con las claves canónicas de <see cref="ClavesDeResumen"/>.
/// </summary>
/// <param name="Bruto">Bruto de incidencias.</param>
/// <param name="TotalPercepciones">Total de percepciones del recibo.</param>
/// <param name="TotalDeducciones">Total de deducciones del recibo.</param>
/// <param name="Neto">Neto pagado.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Subsidio">Subsidio al empleo entregado.</param>
/// <param name="ImssTrabajador">Cuota obrera descontada.</param>
/// <param name="ImssPatronal">Cuotas patronales IMSS.</param>
/// <param name="InfonavitPatronal">Aportación patronal INFONAVIT.</param>
/// <param name="InfonavitTrabajador">Descuento INFONAVIT al trabajador.</param>
/// <param name="Fonacot">Descuento FONACOT.</param>
/// <param name="Isn">Impuesto sobre nóminas.</param>
/// <param name="ComplementoSindical">Complemento pagado vía sindicato.</param>
/// <param name="Facturable">Base facturable al cliente.</param>
/// <param name="Comision">Comisión al cliente.</param>
/// <param name="CostoTotal">Costo total antes de IVA.</param>
/// <param name="CostoIsr">ISR trasladado en la factura.</param>
/// <param name="CostoImss">Cuotas IMSS trasladadas en la factura.</param>
/// <param name="CostoInfonavit">Retiro, cesantía e INFONAVIT trasladados en la factura.</param>
/// <param name="CostoOtros">Otros costos facturables.</param>
public sealed record ResumenDeResultado(
    decimal Bruto,
    decimal TotalPercepciones,
    decimal TotalDeducciones,
    decimal Neto,
    decimal Isr,
    decimal Subsidio,
    decimal ImssTrabajador,
    decimal ImssPatronal,
    decimal InfonavitPatronal,
    decimal InfonavitTrabajador,
    decimal Fonacot,
    decimal Isn,
    decimal ComplementoSindical,
    decimal Facturable,
    decimal Comision,
    decimal CostoTotal,
    decimal CostoIsr,
    decimal CostoImss,
    decimal CostoInfonavit,
    decimal CostoOtros)
{
    /// <summary>
    /// Construye el resumen a partir de los conceptos calculados.
    /// </summary>
    /// <param name="calculo">Resultado del motor.</param>
    /// <returns>El resumen; los conceptos ausentes se leen como cero.</returns>
    public static ResumenDeResultado Desde(ResultadoDeCalculo calculo)
    {
        ArgumentNullException.ThrowIfNull(calculo);

        return new ResumenDeResultado(
            calculo.Obtener(ClavesDeResumen.BrutoIncidencias),
            calculo.Obtener(ClavesDeResumen.TotalPercepciones),
            calculo.Obtener(ClavesDeResumen.TotalDeducciones),
            calculo.Obtener(ClavesDeResumen.NetoPagado),
            calculo.Obtener(ClavesDeResumen.Isr),
            calculo.Obtener(ClavesDeResumen.SubsidioEntregado),
            calculo.Obtener(ClavesDeResumen.ImssTrabajador),
            calculo.Obtener(ClavesDeResumen.ImssPatronal),
            calculo.Obtener(ClavesDeResumen.InfonavitPatronal),
            calculo.Obtener(ClavesDeResumen.InfonavitTrabajador),
            calculo.Obtener(ClavesDeResumen.Fonacot),
            calculo.Obtener(ClavesDeResumen.Isn),
            calculo.Obtener(ClavesDeResumen.ComplementoSindical),
            calculo.Obtener(ClavesDeResumen.TotalNominaFacturable),
            calculo.Obtener(ClavesDeResumen.Comision),
            calculo.Obtener(ClavesDeResumen.CostoTotal),
            calculo.Obtener(ClavesDeResumen.CostoIsr),
            calculo.Obtener(ClavesDeResumen.CostoImss),
            calculo.Obtener(ClavesDeResumen.CostoInfonavit),
            calculo.Obtener(ClavesDeResumen.CostoOtros));
    }
}
