using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Auditoria;

/// <summary>
/// Proyección de lectura de un asiento de la bitácora de auditoría.
/// </summary>
/// <param name="Id">Identificador único del asiento.</param>
/// <param name="Momento">Instante en que ocurrió la acción, en UTC.</param>
/// <param name="Accion">Acción registrada.</param>
/// <param name="UsuarioNombre">Nombre del usuario que ejecutó la acción.</param>
/// <param name="EmpresaRazonSocial">Empresa en cuyo ámbito ocurrió, si aplica.</param>
/// <param name="RecursoTipo">Tipo de recurso afectado.</param>
/// <param name="RecursoId">Identificador del recurso afectado, si aplica.</param>
/// <param name="Exito">Indica si la acción se completó o fue rechazada.</param>
/// <param name="Detalle">Información complementaria, libre de datos personales.</param>
public sealed record RegistroAuditoriaDto(
    Guid Id,
    DateTimeOffset Momento,
    AccionAuditada Accion,
    string UsuarioNombre,
    string? EmpresaRazonSocial,
    string RecursoTipo,
    Guid? RecursoId,
    bool Exito,
    string? Detalle);
