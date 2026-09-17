using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Empresas;

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
/// <param name="TasaIva">Tasa de IVA de la factura, como fracción, o <c>null</c> para usar la general del catálogo.</param>
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
    decimal? TasaIva,
    decimal PorcentajeOtrosCostos,
    decimal? PrimaDeRiesgo,
    string? BancoDispersor,
    bool Activa);
