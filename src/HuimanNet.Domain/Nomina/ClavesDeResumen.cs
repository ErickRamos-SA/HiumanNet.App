using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Claves de concepto que el sistema lee para construir los resúmenes de una
/// corrida, con independencia de cómo estén escritas sus fórmulas.
/// </summary>
/// <remarks>
/// Las fórmulas del catálogo pueden cambiar libremente, pero los conceptos con
/// estas claves deben existir para que los totales, la facturación y el cotejo
/// tengan de dónde leer. Si alguno falta, el resumen lo reporta como cero y la
/// corrida registra una advertencia.
/// </remarks>
public static class ClavesDeResumen
{
    /// <summary>Total bruto de incidencias (sueldo real y percepciones menos descuentos personales).</summary>
    public const string BrutoIncidencias = "BRUTO_INCIDENCIAS";

    /// <summary>Total de percepciones del recibo (fiscal o del esquema).</summary>
    public const string TotalPercepciones = "TOTAL_PERCEPCIONES";

    /// <summary>Total de deducciones del recibo.</summary>
    public const string TotalDeducciones = "TOTAL_DEDUCCIONES";

    /// <summary>Neto pagado del recibo.</summary>
    public const string NetoPagado = "NETO_PAGADO";

    /// <summary>ISR retenido.</summary>
    public const string Isr = "ISR";

    /// <summary>Subsidio al empleo entregado al trabajador.</summary>
    public const string SubsidioEntregado = "SUBSIDIO_ENTREGADO";

    /// <summary>Cuota obrera IMSS descontada al trabajador.</summary>
    public const string ImssTrabajador = "IMSS_TRABAJADOR";

    /// <summary>Cuotas patronales IMSS del período (todos los ramos).</summary>
    public const string ImssPatronal = "IMSS_PATRONAL_TOTAL";

    /// <summary>Aportación patronal INFONAVIT.</summary>
    public const string InfonavitPatronal = "INFONAVIT_PATRONAL";

    /// <summary>Descuento INFONAVIT al trabajador.</summary>
    public const string InfonavitTrabajador = "INFONAVIT_TRABAJADOR";

    /// <summary>Descuento FONACOT.</summary>
    public const string Fonacot = "FONACOT";

    /// <summary>Impuesto sobre nóminas.</summary>
    public const string Isn = "ISN";

    /// <summary>Complemento pagado vía sindicato.</summary>
    public const string ComplementoSindical = "COMPLEMENTO_SINDICAL";

    /// <summary>Base facturable al cliente.</summary>
    public const string TotalNominaFacturable = "TOTAL_NOMINA_FACTURABLE";

    /// <summary>Comisión al cliente.</summary>
    public const string Comision = "COMISION";

    /// <summary>Costo total del trabajador para el cliente, antes de IVA.</summary>
    public const string CostoTotal = "COSTO_TOTAL";

    /// <summary>ISR que se traslada al cliente en la factura (cero cuando la razón social lo absorbe).</summary>
    public const string CostoIsr = "COSTO_ISR";

    /// <summary>Cuotas IMSS que se trasladan al cliente en la factura.</summary>
    public const string CostoImss = "COSTO_IMSS";

    /// <summary>Retiro, cesantía e INFONAVIT que se trasladan al cliente en la factura.</summary>
    public const string CostoInfonavit = "COSTO_INFONAVIT";

    /// <summary>Otros costos facturables.</summary>
    public const string CostoOtros = "COSTO_OTROS";

    /// <summary>Claves que todo esquema debe definir: los totales del recibo y el costo.</summary>
    private static readonly string[] TotalesDelRecibo =
        [BrutoIncidencias, TotalPercepciones, TotalDeducciones, NetoPagado, CostoTotal];

    /// <summary>
    /// Obtiene todas las claves del resumen.
    /// </summary>
    /// <value>Lista de sólo lectura con las claves canónicas.</value>
    public static IReadOnlyList<string> Todas { get; } =
    [
        BrutoIncidencias, TotalPercepciones, TotalDeducciones, NetoPagado, Isr, SubsidioEntregado,
        ImssTrabajador, ImssPatronal, InfonavitPatronal, InfonavitTrabajador, Fonacot, Isn,
        ComplementoSindical, TotalNominaFacturable, Comision, CostoTotal, CostoIsr, CostoImss, CostoInfonavit, CostoOtros,
    ];

    /// <summary>
    /// Obtiene las claves de resumen que el catálogo de un esquema debe definir.
    /// </summary>
    /// <param name="esquema">Esquema de pago.</param>
    /// <returns>
    /// Todas en el esquema IMSS, que factura y lleva cargas patronales; en el
    /// resto, sólo los totales del recibo y el costo.
    /// </returns>
    public static IReadOnlyList<string> ExigidasPara(EsquemaDePago esquema)
        => esquema == EsquemaDePago.Imss ? Todas : TotalesDelRecibo;

    /// <summary>
    /// Lista las claves de resumen exigidas que un cálculo no produjo.
    /// </summary>
    /// <param name="calculo">Resultado del motor.</param>
    /// <param name="esquema">Esquema con el que se calculó.</param>
    /// <returns>Las claves que faltan; vacía si el catálogo las define todas.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="calculo"/> es <c>null</c>.</exception>
    public static IReadOnlyList<string> Faltantes(ResultadoDeCalculo calculo, EsquemaDePago esquema)
    {
        ArgumentNullException.ThrowIfNull(calculo);
        return [.. ExigidasPara(esquema).Where(clave => !calculo.Contiene(clave))];
    }
}
