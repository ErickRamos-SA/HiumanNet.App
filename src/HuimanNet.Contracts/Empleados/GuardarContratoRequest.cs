using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Empleados;

/// <summary>
/// Petición para crear o actualizar un contrato.
/// </summary>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="RazonSocialId">Razón social pagadora.</param>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="NumeroTrabajador">Número de trabajador.</param>
/// <param name="Puesto">Puesto.</param>
/// <param name="Departamento">Departamento o campaña.</param>
/// <param name="TipoDeContrato">Tipo de contrato.</param>
/// <param name="SueldoPeriodoReal">Sueldo real por período.</param>
/// <param name="SalarioDiarioFiscal">Salario diario registrado ante el IMSS.</param>
/// <param name="SalarioDiarioIntegrado">Salario diario integrado.</param>
/// <param name="Zona">Zona de salario mínimo.</param>
/// <param name="InfonavitTipo">Modalidad del crédito INFONAVIT.</param>
/// <param name="InfonavitValor">Valor del aviso INFONAVIT.</param>
/// <param name="InfonavitSeguroVivienda">Seguro de vivienda bimestral.</param>
/// <param name="FonacotMensual">Importe mensual FONACOT.</param>
/// <param name="PensionAlimenticiaImporte">Pensión alimenticia fija.</param>
/// <param name="PensionAlimenticiaPorcentaje">Pensión alimenticia como fracción.</param>
/// <param name="PrestamoPersonalFijo">Préstamo personal fijo.</param>
/// <param name="BonoFijo">Bono fijo.</param>
/// <param name="HonorariosAplicaIva">Si traslada IVA.</param>
/// <param name="PagaComplementoSindical">Si la diferencia con el sueldo real se paga vía sindicato (sueldo mixto).</param>
/// <param name="FechaAlta">Fecha de alta.</param>
/// <param name="FechaBaja">Fecha de baja, o <c>null</c>.</param>
public sealed record GuardarContratoRequest(
    Guid? EmpresaId,
    Guid RazonSocialId,
    EsquemaDePago Esquema,
    string? NumeroTrabajador,
    string? Puesto,
    string? Departamento,
    string? TipoDeContrato,
    decimal SueldoPeriodoReal,
    decimal SalarioDiarioFiscal,
    decimal SalarioDiarioIntegrado,
    ZonaSalarioMinimo Zona,
    TipoDeCreditoInfonavit InfonavitTipo,
    decimal InfonavitValor,
    decimal InfonavitSeguroVivienda,
    decimal FonacotMensual,
    decimal PensionAlimenticiaImporte,
    decimal PensionAlimenticiaPorcentaje,
    decimal PrestamoPersonalFijo,
    decimal BonoFijo,
    bool HonorariosAplicaIva,
    bool PagaComplementoSindical,
    DateOnly FechaAlta,
    DateOnly? FechaBaja);
