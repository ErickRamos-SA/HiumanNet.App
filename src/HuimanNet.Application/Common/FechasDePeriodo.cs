using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Common;

/// <summary>
/// Reglas para derivar fechas de referencia de un período de nómina.
/// </summary>
/// <remarks>
/// El período se identifica por año, mes y consecutivo; el cálculo necesita una
/// fecha concreta para resolver las vigencias del catálogo y de los contratos.
/// Se usa el <b>último día del mes</b> del período: las tablas y parámetros
/// fiscales cambian por ejercicio o por mes, nunca a mitad de semana, y el
/// último día garantiza que un contrato dado de alta durante el mes entre en
/// el cálculo.
/// </remarks>
public static class FechasDePeriodo
{
    /// <summary>
    /// Devuelve la fecha de referencia de un período.
    /// </summary>
    /// <param name="periodo">Período de nómina.</param>
    /// <returns>El último día del mes del período.</returns>
    public static DateOnly Referencia(PeriodoCarga periodo)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        int anio = periodo.Calendario.Anio;
        int mes = periodo.Calendario.Mes;
        return new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));
    }
}
