using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Inicio;

/// <summary>
/// Período no cerrado, con lo necesario para decidir si genera un pendiente.
/// </summary>
/// <param name="PeriodoId">Período.</param>
/// <param name="EmpresaId">Empresa del período.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="Descripcion">Descripción del período.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa.</param>
/// <param name="DocumentosDelCliente">Incidencias y datos de empleados disponibles.</param>
/// <param name="DocumentosDeResultado">Resultados y ajustes disponibles.</param>
public sealed record PeriodoDeInicio(
    Guid PeriodoId,
    Guid EmpresaId,
    EstadoPeriodo Estado,
    string Descripcion,
    string EmpresaRazonSocial,
    int DocumentosDelCliente,
    int DocumentosDeResultado);
