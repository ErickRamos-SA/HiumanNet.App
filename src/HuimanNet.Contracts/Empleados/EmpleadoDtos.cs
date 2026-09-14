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

/// <summary>
/// Proyección de lectura completa de un empleado, con sus contratos.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="EmpresaId">Empresa cliente.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido.</param>
/// <param name="NombreCompleto">Nombre completo para mostrar.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="Curp">CURP.</param>
/// <param name="Nss">Número de seguridad social.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="Correo">Correo personal.</param>
/// <param name="Telefono">Teléfono.</param>
/// <param name="Activo">Estado.</param>
/// <param name="FechaAlta">Fecha de alta en el sistema.</param>
/// <param name="Contratos">Contratos del empleado.</param>
public sealed record EmpleadoDto(
    Guid Id,
    Guid EmpresaId,
    string Clave,
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string NombreCompleto,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono,
    bool Activo,
    DateTimeOffset FechaAlta,
    IReadOnlyList<ContratoDto> Contratos);

/// <summary>
/// Proyección resumida de un empleado para listados.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="NombreCompleto">Nombre completo.</param>
/// <param name="Activo">Estado.</param>
/// <param name="ContratosActivos">Número de contratos vigentes.</param>
/// <param name="Esquemas">Esquemas de sus contratos vigentes, separados por coma.</param>
/// <param name="RazonesSociales">Razones sociales de sus contratos vigentes, separadas por coma.</param>
/// <param name="EmpresaId">Empresa a la que pertenece el empleado.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa, para los listados de varias empresas.</param>
public sealed record EmpleadoResumenDto(
    Guid Id,
    string Clave,
    string NombreCompleto,
    bool Activo,
    int ContratosActivos,
    string Esquemas,
    string RazonesSociales,
    Guid EmpresaId,
    string EmpresaRazonSocial);

/// <summary>
/// Petición para crear o actualizar un empleado.
/// </summary>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Clave">Clave del empleado.</param>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="Curp">CURP.</param>
/// <param name="Nss">Número de seguridad social.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="Correo">Correo personal.</param>
/// <param name="Telefono">Teléfono.</param>
/// <param name="Activo">Estado.</param>
public sealed record GuardarEmpleadoRequest(
    Guid? EmpresaId,
    string Clave,
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono,
    bool Activo = true);

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
