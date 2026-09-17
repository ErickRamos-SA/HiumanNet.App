namespace HuimanNet.Domain.Entities;

/// <summary>
/// Totales consolidados de una corrida de nómina.
/// </summary>
/// <param name="Trabajadores">Número de contratos calculados.</param>
/// <param name="Bruto">Suma del bruto de incidencias.</param>
/// <param name="Percepciones">Suma de percepciones del recibo.</param>
/// <param name="Deducciones">Suma de deducciones del recibo.</param>
/// <param name="Neto">Suma del neto pagado.</param>
/// <param name="Isr">Suma del ISR retenido.</param>
/// <param name="ImssTrabajador">Suma de la cuota obrera descontada.</param>
/// <param name="ImssPatronal">Suma de cuotas patronales IMSS.</param>
/// <param name="Infonavit">Suma de aportaciones patronales INFONAVIT.</param>
/// <param name="Isn">Suma del impuesto sobre nóminas.</param>
/// <param name="ComplementoSindical">Suma del complemento sindical.</param>
/// <param name="Facturable">Suma de la base facturable.</param>
/// <param name="Comision">Suma de comisiones.</param>
/// <param name="CostoTotal">Suma del costo total antes de IVA.</param>
public sealed record TotalesDeCorrida(
    int Trabajadores,
    decimal Bruto,
    decimal Percepciones,
    decimal Deducciones,
    decimal Neto,
    decimal Isr,
    decimal ImssTrabajador,
    decimal ImssPatronal,
    decimal Infonavit,
    decimal Isn,
    decimal ComplementoSindical,
    decimal Facturable,
    decimal Comision,
    decimal CostoTotal)
{
    /// <summary>Obtiene los totales en cero.</summary>
    /// <value>Instancia con todos los importes en cero.</value>
    public static TotalesDeCorrida Vacios { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Consolida los resúmenes de los resultados de una corrida.
    /// </summary>
    /// <param name="resultados">Resultados calculados, uno por contrato.</param>
    /// <returns>Los totales; el número de trabajadores es el de resultados.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="resultados"/> es <c>null</c>.</exception>
    public static TotalesDeCorrida Sumar(IReadOnlyCollection<ResultadoDeNomina> resultados)
    {
        ArgumentNullException.ThrowIfNull(resultados);

        decimal bruto = 0, percepciones = 0, deducciones = 0, neto = 0, isr = 0, imssTrabajador = 0, imssPatronal = 0,
            infonavit = 0, isn = 0, complemento = 0, facturable = 0, comision = 0, costo = 0;

        foreach (ResultadoDeNomina resultado in resultados)
        {
            ResumenDeResultado r = resultado.Resumen;
            bruto += r.Bruto;
            percepciones += r.TotalPercepciones;
            deducciones += r.TotalDeducciones;
            neto += r.Neto;
            isr += r.Isr;
            imssTrabajador += r.ImssTrabajador;
            imssPatronal += r.ImssPatronal;
            infonavit += r.InfonavitPatronal;
            isn += r.Isn;
            complemento += r.ComplementoSindical;
            facturable += r.Facturable;
            comision += r.Comision;
            costo += r.CostoTotal;
        }

        return new TotalesDeCorrida(
            resultados.Count, bruto, percepciones, deducciones, neto, isr, imssTrabajador, imssPatronal,
            infonavit, isn, complemento, facturable, comision, costo);
    }
}
