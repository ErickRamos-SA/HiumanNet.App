namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Variables de entrada que el motor pone a disposición de las fórmulas del
/// catálogo para cada trabajador y período.
/// </summary>
/// <remarks>
/// Ninguna de estas variables contiene un valor fijo: todas se alimentan del
/// contrato, de las incidencias capturadas, de la razón social o de los
/// parámetros del catálogo. Los nombres son parte del contrato con las fórmulas
/// del catálogo, por lo que no deben renombrarse sin migrar el catálogo.
/// </remarks>
public static class VariablesDeCalculo
{
    // ---- Contrato -------------------------------------------------------

    /// <summary>Sueldo real pactado para el período completo (por ejemplo, semanal).</summary>
    public const string SueldoPeriodoReal = "SUELDO_PERIODO_REAL";

    /// <summary>Salario diario registrado ante el IMSS (base fiscal).</summary>
    public const string SalarioDiarioFiscal = "SALARIO_DIARIO_FISCAL";

    /// <summary>Salario diario integrado (SDI) registrado ante el IMSS.</summary>
    public const string Sdi = "SDI";

    /// <summary>1 si el trabajador está en la zona A de salario mínimo; 0 en caso contrario.</summary>
    public const string ZonaA = "ZONA_A";

    /// <summary>1 si el trabajador está en la zona B (frontera norte); 0 en caso contrario.</summary>
    public const string ZonaB = "ZONA_B";

    /// <summary>Modalidad del crédito INFONAVIT: 0 sin crédito, 1 cuota fija, 2 veces salario mínimo, 3 porcentaje.</summary>
    public const string InfonavitTipo = "INFONAVIT_TIPO";

    /// <summary>Valor del aviso INFONAVIT según su modalidad (importe mensual, factor VSM o porcentaje).</summary>
    public const string InfonavitValor = "INFONAVIT_VALOR";

    /// <summary>Seguro de vivienda bimestral del crédito INFONAVIT.</summary>
    public const string InfonavitSeguroVivienda = "INFONAVIT_SEGURO_VIVIENDA";

    /// <summary>Importe mensual autorizado del crédito FONACOT.</summary>
    public const string FonacotMensual = "FONACOT_MENSUAL";

    /// <summary>Importe fijo de pensión alimenticia por período.</summary>
    public const string PensionAlimenticiaImporte = "PENSION_ALIMENTICIA_IMPORTE";

    /// <summary>Porcentaje de pensión alimenticia sobre las percepciones (0 a 1).</summary>
    public const string PensionAlimenticiaPorcentaje = "PENSION_ALIMENTICIA_PORCENTAJE";

    /// <summary>1 si el contrato por honorarios traslada IVA; 0 en caso contrario.</summary>
    public const string HonorariosAplicaIva = "HONORARIOS_APLICA_IVA";

    /// <summary>1 si el contrato IMSS paga la diferencia con el sueldo real vía sindicato (sueldo mixto); 0 en caso contrario.</summary>
    public const string ComplementoSindicalAplica = "COMPLEMENTO_SINDICAL_APLICA";

    /// <summary>Antigüedad del trabajador en años completos a la fecha del período.</summary>
    public const string AntiguedadAnios = "ANTIGUEDAD_ANIOS";

    // ---- Razón social ---------------------------------------------------

    /// <summary>1 si el tipo de servicio es Maquila; 0 si es Nómina.</summary>
    public const string EsMaquila = "ES_MAQUILA";

    /// <summary>1 si la empresa absorbe el subsidio al empleo en el complemento sindical.</summary>
    public const string SubsidioAbsorbido = "SUBSIDIO_ABSORBIDO";

    /// <summary>1 si las faltas se aplican con el factor proporcional (séptimo día).</summary>
    public const string AplicaFaltasProporcionales = "APLICA_FALTAS_PROPORCIONALES";

    /// <summary>1 si la comisión se calcula sobre el costo; 0 si se calcula sobre brutos.</summary>
    public const string ComisionSobreCosto = "COMISION_SOBRE_COSTO";

    /// <summary>Porcentaje de comisión pactado con el cliente (0 a 1).</summary>
    public const string PorcentajeComision = "PORCENTAJE_COMISION";

    /// <summary>Tasa de IVA aplicable a la factura de la razón social (0 a 1).</summary>
    public const string TasaIvaFactura = "TASA_IVA_FACTURA";

    /// <summary>Porcentaje de otros costos facturables sobre el neto pagado (0 a 1).</summary>
    public const string PorcentajeOtrosCostos = "PORCENTAJE_OTROS_COSTOS";

    /// <summary>Prima de riesgo de trabajo aplicable (0 a 1).</summary>
    public const string PrimaRiesgo = "PRIMA_RIESGO";

    // ---- Derivadas ------------------------------------------------------

    /// <summary>Salario mínimo diario de la zona del trabajador.</summary>
    public const string SalarioMinimoZona = "SALARIO_MINIMO_ZONA";

    /// <summary>Tasa de ISN que corresponde al trabajador según la regla de la razón social.</summary>
    public const string IsnTasaZona = "ISN_TASA_ZONA";

    // ---- Incidencias ----------------------------------------------------

    /// <summary>Días del período de pago (7 en nómina semanal).</summary>
    public const string DiasPeriodo = "DIAS_PERIODO";

    /// <summary>Días de vacaciones disfrutados en el período.</summary>
    public const string Vacaciones = "VACACIONES";

    /// <summary>Días de ausentismo del período.</summary>
    public const string Ausentismos = "AUSENTISMOS";

    /// <summary>Días de incapacidad del período.</summary>
    public const string Incapacidades = "INCAPACIDADES";

    /// <summary>Días festivos laborados.</summary>
    public const string Festivos = "FESTIVOS";

    /// <summary>Horas extra dobles.</summary>
    public const string HorasDobles = "HORAS_DOBLES";

    /// <summary>Horas extra triples.</summary>
    public const string HorasTriples = "HORAS_TRIPLES";

    /// <summary>Domingos trabajados (prima dominical).</summary>
    public const string Domingos = "DOMINGOS";

    /// <summary>Gratificaciones y bonos del período (incluye el bono fijo del contrato).</summary>
    public const string Gratificacion = "GRATIFICACION";

    /// <summary>Reembolsos y otros importes devueltos al trabajador.</summary>
    public const string Reembolsos = "REEMBOLSOS";

    /// <summary>Apoyo por teletrabajo.</summary>
    public const string Teletrabajo = "TELETRABAJO";

    /// <summary>Importe de finiquito (sólo en la última nómina).</summary>
    public const string Finiquito = "FINIQUITO";

    /// <summary>Gastos de cafetería a descontar.</summary>
    public const string Cafeteria = "CAFETERIA";

    /// <summary>Horas a descontar.</summary>
    public const string HorasDescontadas = "HORAS_DESCONTADAS";

    /// <summary>Otros descuentos personales del período.</summary>
    public const string OtrosDescuentos = "OTROS_DESCUENTOS";

    /// <summary>Descuento de préstamo personal del período (incluye el fijo del contrato).</summary>
    public const string PrestamoPersonal = "PRESTAMO_PERSONAL";

    /// <summary>Aguinaldo pagado en el período.</summary>
    public const string Aguinaldo = "AGUINALDO";

    /// <summary>Otros descuentos aplicados en el recibo fiscal.</summary>
    public const string DescuentosFiscales = "DESCUENTOS_FISCALES";

    /// <summary>Descuento FONACOT capturado manualmente para el período.</summary>
    public const string FonacotCapturado = "FONACOT_CAPTURADO";

    /// <summary>Descuento adicional aplicado dentro del complemento sindical.</summary>
    public const string DescuentoSindicalAdicional = "DESCUENTO_SINDICAL_ADICIONAL";

    /// <summary>Ajuste manual (positivo o negativo) al complemento sindical.</summary>
    public const string AjusteSindical = "AJUSTE_SINDICAL";

    /// <summary>1 si se capturó una retención manual de ISR; 0 en caso contrario.</summary>
    public const string IsrManualAplica = "ISR_MANUAL_APLICA";

    /// <summary>Importe de la retención manual de ISR, cuando aplica.</summary>
    public const string IsrManual = "ISR_MANUAL";

    /// <summary>1 si el movimiento del período es un finiquito; 0 si es nómina ordinaria.</summary>
    public const string EsFiniquito = "ES_FINIQUITO";

    private static readonly DescripcionDeVariable[] Catalogo =
    [
        new(SueldoPeriodoReal, "Sueldo real pactado para el período completo.", OrigenDeVariable.Contrato),
        new(SalarioDiarioFiscal, "Salario diario registrado ante el IMSS (base fiscal).", OrigenDeVariable.Contrato),
        new(Sdi, "Salario diario integrado registrado ante el IMSS.", OrigenDeVariable.Contrato),
        new(ZonaA, "1 si el trabajador está en la zona A de salario mínimo.", OrigenDeVariable.Contrato),
        new(ZonaB, "1 si el trabajador está en la zona B (frontera norte).", OrigenDeVariable.Contrato),
        new(InfonavitTipo, "Modalidad del crédito INFONAVIT: 0 ninguno, 1 cuota fija, 2 VSM, 3 porcentaje.", OrigenDeVariable.Contrato),
        new(InfonavitValor, "Valor del aviso INFONAVIT según su modalidad.", OrigenDeVariable.Contrato),
        new(InfonavitSeguroVivienda, "Seguro de vivienda bimestral del crédito INFONAVIT.", OrigenDeVariable.Contrato),
        new(FonacotMensual, "Importe mensual autorizado del crédito FONACOT.", OrigenDeVariable.Contrato),
        new(PensionAlimenticiaImporte, "Importe fijo de pensión alimenticia por período.", OrigenDeVariable.Contrato),
        new(PensionAlimenticiaPorcentaje, "Porcentaje de pensión alimenticia sobre percepciones (0 a 1).", OrigenDeVariable.Contrato),
        new(HonorariosAplicaIva, "1 si el contrato por honorarios traslada IVA.", OrigenDeVariable.Contrato),
        new(ComplementoSindicalAplica, "1 si el contrato IMSS paga la diferencia con el sueldo real vía sindicato (sueldo mixto).", OrigenDeVariable.Contrato),
        new(AntiguedadAnios, "Antigüedad en años completos a la fecha del período.", OrigenDeVariable.Contrato),
        new(EsMaquila, "1 si el tipo de servicio de la razón social es Maquila.", OrigenDeVariable.RazonSocial),
        new(SubsidioAbsorbido, "1 si la empresa absorbe el subsidio al empleo.", OrigenDeVariable.RazonSocial),
        new(AplicaFaltasProporcionales, "1 si las faltas se aplican con el factor proporcional.", OrigenDeVariable.RazonSocial),
        new(ComisionSobreCosto, "1 si la comisión se calcula sobre el costo; 0 sobre brutos.", OrigenDeVariable.RazonSocial),
        new(PorcentajeComision, "Porcentaje de comisión pactado (0 a 1).", OrigenDeVariable.RazonSocial),
        new(TasaIvaFactura, "Tasa de IVA de la factura (0 a 1).", OrigenDeVariable.RazonSocial),
        new(PorcentajeOtrosCostos, "Porcentaje de otros costos sobre el neto pagado (0 a 1).", OrigenDeVariable.RazonSocial),
        new(PrimaRiesgo, "Prima de riesgo de trabajo de la razón social o, en su defecto, el parámetro IMSS_PRT_DEFAULT.", OrigenDeVariable.Derivada),
        new(SalarioMinimoZona, "Salario mínimo diario de la zona del trabajador (parámetros SM_ZONA_A / SM_ZONA_B).", OrigenDeVariable.Derivada),
        new(IsnTasaZona, "Tasa de ISN según la zona del trabajador y la regla de la razón social.", OrigenDeVariable.Derivada),
        new(DiasPeriodo, "Días del período de pago.", OrigenDeVariable.Incidencia),
        new(Vacaciones, "Días de vacaciones disfrutados.", OrigenDeVariable.Incidencia),
        new(Ausentismos, "Días de ausentismo.", OrigenDeVariable.Incidencia),
        new(Incapacidades, "Días de incapacidad.", OrigenDeVariable.Incidencia),
        new(Festivos, "Días festivos laborados.", OrigenDeVariable.Incidencia),
        new(HorasDobles, "Horas extra dobles.", OrigenDeVariable.Incidencia),
        new(HorasTriples, "Horas extra triples.", OrigenDeVariable.Incidencia),
        new(Domingos, "Domingos trabajados.", OrigenDeVariable.Incidencia),
        new(Gratificacion, "Gratificaciones y bonos, incluido el bono fijo del contrato.", OrigenDeVariable.Incidencia),
        new(Reembolsos, "Reembolsos y otros importes.", OrigenDeVariable.Incidencia),
        new(Teletrabajo, "Apoyo por teletrabajo.", OrigenDeVariable.Incidencia),
        new(Finiquito, "Importe de finiquito.", OrigenDeVariable.Incidencia),
        new(Cafeteria, "Gastos de cafetería.", OrigenDeVariable.Incidencia),
        new(HorasDescontadas, "Horas a descontar.", OrigenDeVariable.Incidencia),
        new(OtrosDescuentos, "Otros descuentos personales.", OrigenDeVariable.Incidencia),
        new(PrestamoPersonal, "Descuento de préstamo personal, incluido el fijo del contrato.", OrigenDeVariable.Incidencia),
        new(Aguinaldo, "Aguinaldo pagado en el período.", OrigenDeVariable.Incidencia),
        new(DescuentosFiscales, "Otros descuentos del recibo fiscal.", OrigenDeVariable.Incidencia),
        new(FonacotCapturado, "Descuento FONACOT capturado manualmente.", OrigenDeVariable.Incidencia),
        new(DescuentoSindicalAdicional, "Descuento adicional dentro del complemento sindical.", OrigenDeVariable.Incidencia),
        new(AjusteSindical, "Ajuste manual al complemento sindical.", OrigenDeVariable.Incidencia),
        new(IsrManualAplica, "1 si se capturó una retención manual de ISR.", OrigenDeVariable.Incidencia),
        new(IsrManual, "Importe de la retención manual de ISR.", OrigenDeVariable.Incidencia),
        new(EsFiniquito, "1 si el movimiento del período es un finiquito.", OrigenDeVariable.Incidencia),
    ];

    private static readonly HashSet<string> Claves = new(Catalogo.Select(static v => v.Clave), StringComparer.Ordinal);

    /// <summary>
    /// Obtiene la descripción de todas las variables disponibles.
    /// </summary>
    /// <value>Lista de sólo lectura, en el orden en que conviene presentarlas.</value>
    public static IReadOnlyList<DescripcionDeVariable> Todas => Catalogo;

    /// <summary>
    /// Indica si un identificador es una variable de entrada del motor.
    /// </summary>
    /// <param name="clave">Identificador en mayúsculas.</param>
    /// <returns><c>true</c> si es una variable reconocida.</returns>
    public static bool EsVariable(string clave) => Claves.Contains(clave);
}
