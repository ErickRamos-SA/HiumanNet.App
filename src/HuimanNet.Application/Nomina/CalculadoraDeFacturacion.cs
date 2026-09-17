using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Agrupa los resultados de una corrida por razón social y estima la factura
/// con la misma estructura que la hoja de facturación del modelo de referencia.
/// </summary>
/// <remarks>
/// No define ninguna fórmula propia: el subtotal es la suma del concepto
/// <c>COSTO_TOTAL</c> que calcula el catálogo de cada esquema, y las demás
/// columnas sólo desglosan los conceptos que lo componen. El IVA se aplica
/// una vez por factura, con la tasa de la razón social o la general.
/// </remarks>
public static class CalculadoraDeFacturacion
{
    /// <summary>
    /// Calcula la facturación por razón social.
    /// </summary>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="razonesSociales">Razones sociales de la empresa, para el tipo de servicio y la tasa de IVA.</param>
    /// <param name="tasaIvaGeneral">Tasa del catálogo para las razones sociales que no definen la suya.</param>
    /// <returns>Una fila por razón social con resultados.</returns>
    public static IReadOnlyList<FacturacionDeCorridaDto> Calcular(
        IReadOnlyList<ResultadoDeNominaDto> resultados, IReadOnlyList<RazonSocial> razonesSociales, decimal tasaIvaGeneral)
    {
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(razonesSociales);

        var configuracion = razonesSociales.ToDictionary(static r => r.Id);
        var filas = new List<FacturacionDeCorridaDto>();

        foreach (IGrouping<Guid, ResultadoDeNominaDto> grupo in resultados.GroupBy(static r => r.RazonSocialId))
        {
            configuracion.TryGetValue(grupo.Key, out RazonSocial? razonSocial);

            decimal baseNomina = 0, baseFiniquitos = 0, isn = 0, isr = 0, imss = 0, infonavit = 0, otros = 0, comision = 0, subtotal = 0;
            int trabajadores = 0;

            foreach (ResultadoDeNominaDto r in grupo)
            {
                trabajadores++;

                if (r.TipoDeMovimiento == TipoDeMovimiento.Finiquito)
                {
                    baseFiniquitos += r.Facturable;
                }
                else
                {
                    baseNomina += r.Facturable;
                }

                isn += r.Isn;
                isr += r.CostoIsr;
                imss += r.CostoImss;
                infonavit += r.CostoInfonavit;
                otros += r.CostoOtros;
                comision += r.Comision;
                subtotal += r.CostoTotal;
            }

            decimal tasaIva = razonSocial?.Configuracion.TasaIva ?? tasaIvaGeneral;
            decimal iva = Math.Round(subtotal * tasaIva, 2, MidpointRounding.AwayFromZero);

            filas.Add(new FacturacionDeCorridaDto(
                grupo.Key,
                razonSocial?.Nombre ?? grupo.First().RazonSocialNombre,
                razonSocial?.Configuracion.TipoDeServicio ?? TipoDeServicio.NoEspecificado,
                trabajadores, baseNomina, baseFiniquitos, isn, isr, imss, infonavit, otros, comision,
                subtotal, iva, subtotal + iva));
        }

        return filas.OrderBy(static f => f.RazonSocialNombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
