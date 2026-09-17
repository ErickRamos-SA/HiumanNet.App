using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Cantidades e importes capturados para un contrato en un período.
/// </summary>
/// <param name="DiasPeriodo">Días del período de pago.</param>
/// <param name="Vacaciones">Días de vacaciones.</param>
/// <param name="Ausentismos">Días de ausentismo.</param>
/// <param name="Incapacidades">Días de incapacidad.</param>
/// <param name="Festivos">Días festivos laborados.</param>
/// <param name="HorasDobles">Horas extra dobles.</param>
/// <param name="HorasTriples">Horas extra triples.</param>
/// <param name="DomingosTrabajados">Domingos trabajados.</param>
/// <param name="Gratificacion">Gratificaciones y bonos del período.</param>
/// <param name="Reembolsos">Reembolsos y otros.</param>
/// <param name="Teletrabajo">Apoyo por teletrabajo.</param>
/// <param name="Finiquito">Importe de finiquito.</param>
/// <param name="Cafeteria">Gastos de cafetería.</param>
/// <param name="HorasDescontadas">Horas a descontar.</param>
/// <param name="OtrosDescuentos">Otros descuentos personales.</param>
/// <param name="PrestamoPersonal">Descuento de préstamo personal del período.</param>
/// <param name="Aguinaldo">Aguinaldo pagado en el período.</param>
/// <param name="DescuentosFiscales">Otros descuentos del recibo fiscal.</param>
/// <param name="FonacotCapturado">Descuento FONACOT capturado manualmente.</param>
/// <param name="DescuentoSindicalAdicional">Descuento adicional dentro del complemento sindical.</param>
/// <param name="AjusteSindical">Ajuste manual al complemento sindical (positivo o negativo).</param>
/// <param name="IsrManual">Retención manual de ISR, o <c>null</c> para calcularla.</param>
/// <param name="TipoDeMovimiento">Nómina ordinaria o finiquito.</param>
/// <param name="Observaciones">Nota libre; documenta los ajustes manuales y su vigencia.</param>
public sealed record DatosDeIncidencia(
    decimal DiasPeriodo,
    decimal Vacaciones,
    decimal Ausentismos,
    decimal Incapacidades,
    decimal Festivos,
    decimal HorasDobles,
    decimal HorasTriples,
    decimal DomingosTrabajados,
    decimal Gratificacion,
    decimal Reembolsos,
    decimal Teletrabajo,
    decimal Finiquito,
    decimal Cafeteria,
    decimal HorasDescontadas,
    decimal OtrosDescuentos,
    decimal PrestamoPersonal,
    decimal Aguinaldo,
    decimal DescuentosFiscales,
    decimal FonacotCapturado,
    decimal DescuentoSindicalAdicional,
    decimal AjusteSindical,
    decimal? IsrManual,
    TipoDeMovimiento TipoDeMovimiento,
    string? Observaciones)
{
    /// <summary>
    /// Crea una incidencia de semana completa sin novedades.
    /// </summary>
    /// <param name="diasPeriodo">Días del período de pago.</param>
    /// <returns>Datos con todas las cantidades en cero salvo los días del período.</returns>
    public static DatosDeIncidencia SinNovedades(decimal diasPeriodo)
        => new(diasPeriodo, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, TipoDeMovimiento.Ordinaria, null);
}
