using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Resumen del resultado de un contrato dentro de una corrida.
/// </summary>
/// <param name="Id">Identificador del resultado.</param>
/// <param name="CorridaId">Corrida.</param>
/// <param name="ContratoId">Contrato.</param>
/// <param name="EmpleadoId">Empleado.</param>
/// <param name="RazonSocialId">Razón social.</param>
/// <param name="RazonSocialNombre">Nombre de la razón social.</param>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="ClaveEmpleado">Clave del empleado.</param>
/// <param name="NombreEmpleado">Nombre del empleado.</param>
/// <param name="TipoDeMovimiento">Ordinaria o finiquito.</param>
/// <param name="Bruto">Bruto de incidencias.</param>
/// <param name="TotalPercepciones">Percepciones del recibo.</param>
/// <param name="TotalDeducciones">Deducciones del recibo.</param>
/// <param name="Neto">Neto pagado.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Subsidio">Subsidio entregado.</param>
/// <param name="ImssTrabajador">Cuota obrera.</param>
/// <param name="ImssPatronal">Cuotas patronales.</param>
/// <param name="InfonavitPatronal">INFONAVIT patronal.</param>
/// <param name="InfonavitTrabajador">INFONAVIT trabajador.</param>
/// <param name="Fonacot">FONACOT.</param>
/// <param name="Isn">ISN.</param>
/// <param name="ComplementoSindical">Complemento sindical.</param>
/// <param name="Facturable">Base facturable.</param>
/// <param name="Comision">Comisión.</param>
/// <param name="CostoTotal">Costo total antes de IVA.</param>
/// <param name="CostoIsr">ISR trasladado en la factura.</param>
/// <param name="CostoImss">Cuotas IMSS trasladadas en la factura.</param>
/// <param name="CostoInfonavit">Retiro, cesantía e INFONAVIT trasladados en la factura.</param>
/// <param name="CostoOtros">Otros costos facturables.</param>
/// <param name="Advertencia">Advertencia del motor, o <c>null</c>.</param>
public sealed record ResultadoDeNominaDto(
    Guid Id,
    Guid CorridaId,
    Guid ContratoId,
    Guid EmpleadoId,
    Guid RazonSocialId,
    string RazonSocialNombre,
    EsquemaDePago Esquema,
    string ClaveEmpleado,
    string NombreEmpleado,
    TipoDeMovimiento TipoDeMovimiento,
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
    decimal CostoOtros,
    string? Advertencia);
