using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Periodos;

/// <summary>
/// Proyección de lectura de un período de carga, con el resumen de documentos
/// que necesitan las bandejas de cliente y operador.
/// </summary>
/// <param name="Id">Identificador único del período.</param>
/// <param name="EmpresaId">Empresa propietaria.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa, para la bandeja del operador.</param>
/// <param name="Clave">Clave canónica del período, en formato <c>aaaa-MM-cc</c>.</param>
/// <param name="Descripcion">Descripción legible del período.</param>
/// <param name="Estado">Estado del ciclo de intercambio.</param>
/// <param name="FechaApertura">Instante de apertura, en UTC.</param>
/// <param name="FechaLimiteCarga">Fecha límite para las cargas del cliente, si se fijó.</param>
/// <param name="FechaCierre">Instante de cierre, si el período ya se cerró.</param>
/// <param name="DocumentosDelCliente">Documentos disponibles aportados por la empresa cliente.</param>
/// <param name="DocumentosDeResultado">Documentos de resultado y ajuste disponibles.</param>
public sealed record PeriodoDto(
    Guid Id,
    Guid EmpresaId,
    string EmpresaRazonSocial,
    string Clave,
    string Descripcion,
    EstadoPeriodo Estado,
    DateTimeOffset FechaApertura,
    DateTimeOffset? FechaLimiteCarga,
    DateTimeOffset? FechaCierre,
    int DocumentosDelCliente,
    int DocumentosDeResultado);
