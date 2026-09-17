using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Empleados;

/// <summary>
/// Proyección de lectura de un contrato.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="EmpleadoId">Empleado.</param>
/// <param name="RazonSocialId">Razón social pagadora.</param>
/// <param name="RazonSocialNombre">Nombre de la razón social.</param>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="NumeroTrabajador">Número de trabajador (NOI).</param>
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
/// <param name="PensionAlimenticiaImporte">Pensión alimenticia fija por período.</param>
/// <param name="PensionAlimenticiaPorcentaje">Pensión alimenticia como fracción de las percepciones.</param>
/// <param name="PrestamoPersonalFijo">Préstamo personal fijo por período.</param>
/// <param name="BonoFijo">Bono fijo por período.</param>
/// <param name="HonorariosAplicaIva">Si traslada IVA (honorarios).</param>
/// <param name="PagaComplementoSindical">Si la diferencia con el sueldo real se paga vía sindicato (sueldo mixto).</param>
/// <param name="FechaAlta">Fecha de alta.</param>
/// <param name="FechaBaja">Fecha de baja, o <c>null</c>.</param>
/// <param name="Activo">Si el contrato no tiene baja.</param>
public sealed record ContratoDto(
    Guid Id,
    Guid EmpleadoId,
    Guid RazonSocialId,
    string RazonSocialNombre,
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
    DateOnly? FechaBaja,
    bool Activo);
