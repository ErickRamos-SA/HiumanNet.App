using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Incidencias;

/// <summary>
/// Petición para capturar o actualizar la incidencia de un contrato en un período.
/// </summary>
/// <param name="PeriodoId">Período.</param>
/// <param name="ContratoId">Contrato.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DiasPeriodo">Días del período.</param>
/// <param name="Vacaciones">Días de vacaciones.</param>
/// <param name="Ausentismos">Días de ausentismo.</param>
/// <param name="Incapacidades">Días de incapacidad.</param>
/// <param name="Festivos">Festivos laborados.</param>
/// <param name="HorasDobles">Horas dobles.</param>
/// <param name="HorasTriples">Horas triples.</param>
/// <param name="DomingosTrabajados">Domingos trabajados.</param>
/// <param name="Gratificacion">Gratificación o bonos.</param>
/// <param name="Reembolsos">Reembolsos.</param>
/// <param name="Teletrabajo">Teletrabajo.</param>
/// <param name="Finiquito">Finiquito.</param>
/// <param name="Cafeteria">Cafetería.</param>
/// <param name="HorasDescontadas">Horas descontadas.</param>
/// <param name="OtrosDescuentos">Otros descuentos.</param>
/// <param name="PrestamoPersonal">Préstamo personal.</param>
/// <param name="Aguinaldo">Aguinaldo.</param>
/// <param name="DescuentosFiscales">Descuentos del recibo fiscal.</param>
/// <param name="FonacotCapturado">FONACOT capturado.</param>
/// <param name="DescuentoSindicalAdicional">Descuento adicional sindical.</param>
/// <param name="AjusteSindical">Ajuste sindical.</param>
/// <param name="IsrManual">ISR manual, o <c>null</c> para calcularlo.</param>
/// <param name="TipoDeMovimiento">Ordinaria o finiquito.</param>
/// <param name="Observaciones">Observaciones.</param>
public sealed record GuardarIncidenciaRequest(
    Guid PeriodoId,
    Guid ContratoId,
    Guid? EmpresaId,
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
    string? Observaciones);
