using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Asiento inmutable de la bitácora de auditoría: quién hizo qué, sobre qué
/// recurso y cuándo.
/// </summary>
/// <remarks>
/// En un portal de datos de nómina, registrar <b>quién descargó</b> es tan
/// importante como registrar quién subió (ARQUITECTURA.md §6.3). Los asientos
/// no se modifican ni se borran: sólo se insertan.
/// <para>
/// El campo <see cref="Detalle"/> nunca debe contener datos personales ni
/// identificadores fiscales (ESPECIFICACION.md §9, seguridad).
/// </para>
/// </remarks>
public sealed class RegistroAuditoria
{
    /// <summary>
    /// Inicializa una instancia con valores ya validados. Sólo la usan las
    /// fábricas y <see cref="Rehidratar"/>.
    /// </summary>
    /// <inheritdoc cref="Rehidratar" path="/param"/>
    private RegistroAuditoria(
        Guid id,
        DateTimeOffset momento,
        AccionAuditada accion,
        Guid usuarioId,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        bool exito,
        string? detalle,
        string? direccionIp)
    {
        Id = id;
        Momento = momento;
        Accion = accion;
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        RecursoTipo = recursoTipo;
        RecursoId = recursoId;
        Exito = exito;
        Detalle = detalle;
        DireccionIp = direccionIp;
    }

    /// <summary>
    /// Obtiene el identificador único del asiento.
    /// </summary>
    /// <value>GUID versión 7: ordenable por tiempo de inserción.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene el instante en que ocurrió la acción.
    /// </summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset Momento { get; }

    /// <summary>
    /// Obtiene la acción registrada.
    /// </summary>
    /// <value>Operación auditada del catálogo <see cref="AccionAuditada"/>.</value>
    public AccionAuditada Accion { get; }

    /// <summary>
    /// Obtiene el usuario que ejecutó la acción.
    /// </summary>
    /// <value>Identificador local del <see cref="Usuario"/>.</value>
    public Guid UsuarioId { get; }

    /// <summary>
    /// Obtiene la empresa en cuyo ámbito ocurrió la acción.
    /// </summary>
    /// <value><c>null</c> para acciones transversales del administrador.</value>
    public Guid? EmpresaId { get; }

    /// <summary>
    /// Obtiene el tipo de recurso afectado.
    /// </summary>
    /// <value>Por ejemplo <c>"Documento"</c>, <c>"Periodo"</c> o <c>"Empresa"</c>.</value>
    public string RecursoTipo { get; }

    /// <summary>
    /// Obtiene el identificador del recurso afectado.
    /// </summary>
    /// <value><c>null</c> si la acción no apunta a un recurso concreto.</value>
    public Guid? RecursoId { get; }

    /// <summary>
    /// Obtiene un valor que indica si la acción se completó correctamente.
    /// </summary>
    /// <value><c>false</c> para intentos rechazados, que también se auditan.</value>
    public bool Exito { get; }

    /// <summary>
    /// Obtiene información complementaria de la acción.
    /// </summary>
    /// <value>Texto técnico y libre de datos personales, o <c>null</c>.</value>
    public string? Detalle { get; }

    /// <summary>
    /// Obtiene la dirección IP de origen de la petición.
    /// </summary>
    /// <value>Cadena con la IP del cliente, o <c>null</c> si no está disponible.</value>
    public string? DireccionIp { get; }

    /// <summary>
    /// Crea un asiento de auditoría para una acción completada con éxito.
    /// </summary>
    /// <param name="accion">Acción ejecutada.</param>
    /// <param name="usuarioId">Usuario responsable.</param>
    /// <param name="empresaId">Empresa en cuyo ámbito ocurrió, si aplica.</param>
    /// <param name="recursoTipo">Tipo de recurso afectado.</param>
    /// <param name="recursoId">Identificador del recurso afectado, si aplica.</param>
    /// <param name="momento">Instante de la acción, en UTC.</param>
    /// <param name="detalle">Información complementaria sin datos personales.</param>
    /// <param name="direccionIp">Dirección IP de origen, si está disponible.</param>
    /// <returns>El asiento listo para persistirse.</returns>
    /// <exception cref="ArgumentException">Se lanza si <paramref name="recursoTipo"/> está vacío.</exception>
    public static RegistroAuditoria Exitoso(
        AccionAuditada accion,
        Guid usuarioId,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        DateTimeOffset momento,
        string? detalle = null,
        string? direccionIp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recursoTipo);

        return new RegistroAuditoria(
            Guid.CreateVersion7(), momento, accion, usuarioId, empresaId,
            recursoTipo, recursoId, exito: true, detalle, direccionIp);
    }

    /// <summary>
    /// Crea un asiento de auditoría para un intento rechazado.
    /// </summary>
    /// <param name="accion">Acción intentada.</param>
    /// <param name="usuarioId">Usuario que la intentó.</param>
    /// <param name="empresaId">Empresa en cuyo ámbito se intentó, si aplica.</param>
    /// <param name="recursoTipo">Tipo de recurso afectado.</param>
    /// <param name="recursoId">Identificador del recurso afectado, si aplica.</param>
    /// <param name="momento">Instante del intento, en UTC.</param>
    /// <param name="motivo">Motivo técnico del rechazo.</param>
    /// <param name="direccionIp">Dirección IP de origen, si está disponible.</param>
    /// <returns>El asiento listo para persistirse.</returns>
    /// <exception cref="ArgumentException">
    /// Se lanza si <paramref name="recursoTipo"/> o <paramref name="motivo"/> están vacíos.
    /// </exception>
    public static RegistroAuditoria Fallido(
        AccionAuditada accion,
        Guid usuarioId,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        DateTimeOffset momento,
        string motivo,
        string? direccionIp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recursoTipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        return new RegistroAuditoria(
            Guid.CreateVersion7(), momento, accion, usuarioId, empresaId,
            recursoTipo, recursoId, exito: false, motivo, direccionIp);
    }

    /// <summary>
    /// Reconstruye un asiento a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="momento">Instante de la acción en UTC.</param>
    /// <param name="accion">Acción registrada.</param>
    /// <param name="usuarioId">Usuario responsable.</param>
    /// <param name="empresaId">Empresa asociada, si aplica.</param>
    /// <param name="recursoTipo">Tipo de recurso afectado.</param>
    /// <param name="recursoId">Identificador del recurso, si aplica.</param>
    /// <param name="exito">Resultado de la acción.</param>
    /// <param name="detalle">Información complementaria.</param>
    /// <param name="direccionIp">Dirección IP de origen.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static RegistroAuditoria Rehidratar(
        Guid id,
        DateTimeOffset momento,
        AccionAuditada accion,
        Guid usuarioId,
        Guid? empresaId,
        string recursoTipo,
        Guid? recursoId,
        bool exito,
        string? detalle,
        string? direccionIp)
        => new(id, momento, accion, usuarioId, empresaId, recursoTipo, recursoId, exito, detalle, direccionIp);
}
