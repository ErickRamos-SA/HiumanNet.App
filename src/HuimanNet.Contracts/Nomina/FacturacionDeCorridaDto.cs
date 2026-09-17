using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Factura estimada de una razón social dentro de una corrida, separando
/// nómina ordinaria y finiquitos.
/// </summary>
/// <param name="RazonSocialId">Razón social.</param>
/// <param name="RazonSocialNombre">Nombre.</param>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="Trabajadores">Contratos incluidos.</param>
/// <param name="BaseNomina">Base facturable de nómina ordinaria.</param>
/// <param name="BaseFiniquitos">Base facturable de finiquitos.</param>
/// <param name="Isn">ISN.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Imss">Cuotas IMSS.</param>
/// <param name="Infonavit">Retiro, cesantía e INFONAVIT.</param>
/// <param name="Otros">Otros costos.</param>
/// <param name="Comision">Comisión.</param>
/// <param name="Subtotal">Subtotal antes de IVA.</param>
/// <param name="Iva">IVA.</param>
/// <param name="Total">Total facturado.</param>
public sealed record FacturacionDeCorridaDto(
    Guid RazonSocialId,
    string RazonSocialNombre,
    TipoDeServicio TipoDeServicio,
    int Trabajadores,
    decimal BaseNomina,
    decimal BaseFiniquitos,
    decimal Isn,
    decimal Isr,
    decimal Imss,
    decimal Infonavit,
    decimal Otros,
    decimal Comision,
    decimal Subtotal,
    decimal Iva,
    decimal Total);
