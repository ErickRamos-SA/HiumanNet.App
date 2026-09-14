using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Empresas;

/// <summary>
/// Proyección de lectura de una empresa cliente.
/// </summary>
/// <param name="Id">Identificador único de la empresa.</param>
/// <param name="RazonSocial">Razón social mostrada en la interfaz.</param>
/// <param name="Activa">Indica si la empresa puede operar en el portal.</param>
/// <remarks>
/// No incluye el identificador fiscal: es un dato sensible y ningún cliente lo
/// necesita para operar el portal de documentos.
/// </remarks>
public sealed record EmpresaDto(Guid Id, string RazonSocial, bool Activa);

/// <summary>
/// Detalle administrativo de una empresa cliente.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">RFC o identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
/// <param name="FechaAlta">Fecha de alta, en UTC.</param>
/// <param name="TotalRazonesSociales">Razones sociales activas.</param>
/// <param name="TotalEmpleados">Empleados activos.</param>
/// <param name="TotalUsuarios">Usuarios activos.</param>
public sealed record EmpresaDetalleDto(
    Guid Id,
    string RazonSocial,
    string IdentificadorFiscal,
    bool Activa,
    DateTimeOffset FechaAlta,
    int TotalRazonesSociales,
    int TotalEmpleados,
    int TotalUsuarios);

/// <summary>
/// Petición para crear o actualizar una empresa cliente.
/// </summary>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">RFC o identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
public sealed record GuardarEmpresaRequest(string RazonSocial, string IdentificadorFiscal, bool Activa = true);

/// <summary>
/// Proyección de lectura de una razón social.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="EmpresaId">Empresa cliente propietaria.</param>
/// <param name="Nombre">Nombre o razón social.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="RegistroPatronal">Registro patronal, si aplica.</param>
/// <param name="Zona">Zona de salario mínimo predeterminada.</param>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="SubsidioAbsorbido">Si absorbe el subsidio al empleo.</param>
/// <param name="AplicaFaltasProporcionales">Si aplica el factor proporcional a las faltas.</param>
/// <param name="ModalidadDeComision">Base de la comisión.</param>
/// <param name="PorcentajeComision">Porcentaje de comisión, como fracción.</param>
/// <param name="ZonaIsn">Regla de la tasa de ISN.</param>
/// <param name="TasaIva">Tasa de IVA de la factura, como fracción.</param>
/// <param name="PorcentajeOtrosCostos">Otros costos sobre el neto, como fracción.</param>
/// <param name="PrimaDeRiesgo">Prima de riesgo, como fracción, o <c>null</c> para usar la general.</param>
/// <param name="BancoDispersor">Banco dispersor.</param>
/// <param name="Activa">Estado.</param>
public sealed record RazonSocialDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string Rfc,
    string? RegistroPatronal,
    ZonaSalarioMinimo Zona,
    TipoDeServicio TipoDeServicio,
    bool SubsidioAbsorbido,
    bool AplicaFaltasProporcionales,
    ModalidadDeComision ModalidadDeComision,
    decimal PorcentajeComision,
    ZonaIsn ZonaIsn,
    decimal TasaIva,
    decimal PorcentajeOtrosCostos,
    decimal? PrimaDeRiesgo,
    string? BancoDispersor,
    bool Activa);

/// <summary>
/// Petición para crear o actualizar una razón social.
/// </summary>
/// <param name="EmpresaId">Empresa propietaria; sólo la aportan los roles transversales.</param>
/// <param name="Nombre">Nombre o razón social.</param>
/// <param name="Rfc">RFC.</param>
/// <param name="RegistroPatronal">Registro patronal, si aplica.</param>
/// <param name="Zona">Zona de salario mínimo predeterminada.</param>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="SubsidioAbsorbido">Si absorbe el subsidio al empleo.</param>
/// <param name="AplicaFaltasProporcionales">Si aplica el factor proporcional a las faltas.</param>
/// <param name="ModalidadDeComision">Base de la comisión.</param>
/// <param name="PorcentajeComision">Porcentaje de comisión, como fracción.</param>
/// <param name="ZonaIsn">Regla de la tasa de ISN.</param>
/// <param name="TasaIva">Tasa de IVA de la factura, como fracción.</param>
/// <param name="PorcentajeOtrosCostos">Otros costos sobre el neto, como fracción.</param>
/// <param name="PrimaDeRiesgo">Prima de riesgo, como fracción, o <c>null</c>.</param>
/// <param name="BancoDispersor">Banco dispersor.</param>
/// <param name="Activa">Estado.</param>
public sealed record GuardarRazonSocialRequest(
    Guid? EmpresaId,
    string Nombre,
    string Rfc,
    string? RegistroPatronal,
    ZonaSalarioMinimo Zona,
    TipoDeServicio TipoDeServicio,
    bool SubsidioAbsorbido,
    bool AplicaFaltasProporcionales,
    ModalidadDeComision ModalidadDeComision,
    decimal PorcentajeComision,
    ZonaIsn ZonaIsn,
    decimal TasaIva,
    decimal PorcentajeOtrosCostos,
    decimal? PrimaDeRiesgo,
    string? BancoDispersor,
    bool Activa = true);
