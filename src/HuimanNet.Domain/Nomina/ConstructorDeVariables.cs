using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Prepara el diccionario de variables de entrada de un contrato para el motor
/// de cálculo, a partir del contrato, su razón social, la incidencia del
/// período y los parámetros vigentes.
/// </summary>
/// <remarks>
/// Servicio de dominio puro: no accede a infraestructura. Toda variable que las
/// fórmulas pueden usar se construye aquí y está documentada en
/// <see cref="VariablesDeCalculo"/>.
/// </remarks>
public static class ConstructorDeVariables
{
    /// <summary>
    /// Construye las variables de un contrato.
    /// </summary>
    /// <param name="contrato">Contrato a calcular.</param>
    /// <param name="razonSocial">Razón social pagadora del contrato.</param>
    /// <param name="incidencia">Incidencia del período, o <c>null</c> para período completo sin novedades.</param>
    /// <param name="parametros">Parámetros vigentes por clave.</param>
    /// <param name="fechaDeReferencia">Fecha del período, para la antigüedad.</param>
    /// <returns>Diccionario de variables por clave en mayúsculas.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si falta un parámetro obligatorio.</exception>
    public static IReadOnlyDictionary<string, decimal> Construir(
        Contrato contrato,
        RazonSocial razonSocial,
        Incidencia? incidencia,
        IReadOnlyDictionary<string, decimal> parametros,
        DateOnly fechaDeReferencia)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        ArgumentNullException.ThrowIfNull(razonSocial);
        ArgumentNullException.ThrowIfNull(parametros);

        CondicionesDeContrato condiciones = contrato.Condiciones;
        ConfiguracionDeRazonSocial configuracion = razonSocial.Configuracion;

        DatosDeIncidencia datos = incidencia?.Datos
            ?? DatosDeIncidencia.SinNovedades(Parametro(parametros, ClavesDeParametro.DiasPeriodoPredeterminados));

        ZonaSalarioMinimo zona = condiciones.Zona == ZonaSalarioMinimo.NoEspecificado ? razonSocial.Zona : condiciones.Zona;
        bool esZonaB = zona == ZonaSalarioMinimo.B;

        decimal salarioMinimo = Parametro(
            parametros, esZonaB ? ClavesDeParametro.SalarioMinimoZonaB : ClavesDeParametro.SalarioMinimoZonaA);

        decimal tasaIsn = configuracion.ZonaIsn switch
        {
            ZonaIsn.ForzarZonaA => Parametro(parametros, ClavesDeParametro.IsnZonaA),
            ZonaIsn.ForzarZonaB => Parametro(parametros, ClavesDeParametro.IsnZonaB),
            _ => Parametro(parametros, esZonaB ? ClavesDeParametro.IsnZonaB : ClavesDeParametro.IsnZonaA),
        };

        decimal primaRiesgo = configuracion.PrimaDeRiesgo
            ?? Parametro(parametros, ClavesDeParametro.PrimaRiesgoPredeterminada);

        decimal tasaIva = configuracion.TasaIva
            ?? Parametro(parametros, ClavesDeParametro.TasaIva);

        var variables = new Dictionary<string, decimal>(64, StringComparer.Ordinal)
        {
            [VariablesDeCalculo.SueldoPeriodoReal] = condiciones.SueldoPeriodoReal,
            [VariablesDeCalculo.SalarioDiarioFiscal] = condiciones.SalarioDiarioFiscal,
            [VariablesDeCalculo.Sdi] = condiciones.SalarioDiarioIntegrado,
            [VariablesDeCalculo.ZonaA] = esZonaB ? 0m : 1m,
            [VariablesDeCalculo.ZonaB] = esZonaB ? 1m : 0m,
            [VariablesDeCalculo.InfonavitTipo] = (decimal)(int)condiciones.Infonavit.Tipo,
            [VariablesDeCalculo.InfonavitValor] = condiciones.Infonavit.Valor,
            [VariablesDeCalculo.InfonavitSeguroVivienda] = condiciones.Infonavit.SeguroDeVivienda,
            [VariablesDeCalculo.FonacotMensual] = condiciones.FonacotMensual,
            [VariablesDeCalculo.PensionAlimenticiaImporte] = condiciones.PensionAlimenticiaImporte,
            [VariablesDeCalculo.PensionAlimenticiaPorcentaje] = condiciones.PensionAlimenticiaPorcentaje,
            [VariablesDeCalculo.HonorariosAplicaIva] = condiciones.HonorariosAplicaIva ? 1m : 0m,
            [VariablesDeCalculo.ComplementoSindicalAplica] = condiciones.PagaComplementoSindical ? 1m : 0m,
            [VariablesDeCalculo.AntiguedadAnios] = contrato.AntiguedadEnAnios(fechaDeReferencia),

            [VariablesDeCalculo.EsMaquila] = configuracion.TipoDeServicio == TipoDeServicio.Maquila ? 1m : 0m,
            [VariablesDeCalculo.SubsidioAbsorbido] = configuracion.SubsidioAbsorbido ? 1m : 0m,
            [VariablesDeCalculo.AplicaFaltasProporcionales] = configuracion.AplicaFaltasProporcionales ? 1m : 0m,
            [VariablesDeCalculo.ComisionSobreCosto] = configuracion.ModalidadDeComision == ModalidadDeComision.SobreCosto ? 1m : 0m,
            [VariablesDeCalculo.PorcentajeComision] = configuracion.PorcentajeComision,
            [VariablesDeCalculo.TasaIvaFactura] = tasaIva,
            [VariablesDeCalculo.PorcentajeOtrosCostos] = configuracion.PorcentajeOtrosCostos,
            [VariablesDeCalculo.PrimaRiesgo] = primaRiesgo,

            [VariablesDeCalculo.SalarioMinimoZona] = salarioMinimo,
            [VariablesDeCalculo.IsnTasaZona] = tasaIsn,

            [VariablesDeCalculo.DiasPeriodo] = datos.DiasPeriodo,
            [VariablesDeCalculo.Vacaciones] = datos.Vacaciones,
            [VariablesDeCalculo.Ausentismos] = datos.Ausentismos,
            [VariablesDeCalculo.Incapacidades] = datos.Incapacidades,
            [VariablesDeCalculo.Festivos] = datos.Festivos,
            [VariablesDeCalculo.HorasDobles] = datos.HorasDobles,
            [VariablesDeCalculo.HorasTriples] = datos.HorasTriples,
            [VariablesDeCalculo.Domingos] = datos.DomingosTrabajados,
            [VariablesDeCalculo.Gratificacion] = datos.Gratificacion + condiciones.BonoFijo,
            [VariablesDeCalculo.Reembolsos] = datos.Reembolsos,
            [VariablesDeCalculo.Teletrabajo] = datos.Teletrabajo,
            [VariablesDeCalculo.Finiquito] = datos.Finiquito,
            [VariablesDeCalculo.Cafeteria] = datos.Cafeteria,
            [VariablesDeCalculo.HorasDescontadas] = datos.HorasDescontadas,
            [VariablesDeCalculo.OtrosDescuentos] = datos.OtrosDescuentos,
            [VariablesDeCalculo.PrestamoPersonal] = datos.PrestamoPersonal + condiciones.PrestamoPersonalFijo,
            [VariablesDeCalculo.Aguinaldo] = datos.Aguinaldo,
            [VariablesDeCalculo.DescuentosFiscales] = datos.DescuentosFiscales,
            [VariablesDeCalculo.FonacotCapturado] = datos.FonacotCapturado,
            [VariablesDeCalculo.DescuentoSindicalAdicional] = datos.DescuentoSindicalAdicional,
            [VariablesDeCalculo.AjusteSindical] = datos.AjusteSindical,
            [VariablesDeCalculo.IsrManualAplica] = datos.IsrManual is null ? 0m : 1m,
            [VariablesDeCalculo.IsrManual] = datos.IsrManual ?? 0m,
            [VariablesDeCalculo.EsFiniquito] = datos.TipoDeMovimiento == TipoDeMovimiento.Finiquito ? 1m : 0m,
        };

        return variables;
    }

    /// <summary>Obtiene un parámetro que el cálculo de variables necesita siempre.</summary>
    /// <param name="parametros">Parámetros vigentes.</param>
    /// <param name="clave">Clave del parámetro.</param>
    /// <returns>El valor del parámetro.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el catálogo no define el parámetro para la fecha.</exception>
    private static decimal Parametro(IReadOnlyDictionary<string, decimal> parametros, string clave)
        => parametros.TryGetValue(clave, out decimal valor)
            ? valor
            : throw new CatalogoInvalidoException(
                $"Falta el parámetro obligatorio '{clave}' en el catálogo para la fecha del período.");
}
