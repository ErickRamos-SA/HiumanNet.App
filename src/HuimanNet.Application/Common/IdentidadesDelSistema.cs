namespace HuimanNet.Application.Common;

/// <summary>
/// Identidades reservadas que el propio sistema usa cuando actúa sin un usuario
/// humano detrás.
/// </summary>
/// <remarks>
/// Existen como filas fijas en <c>dbo.Usuarios</c> para que la bitácora de
/// auditoría nunca contenga asientos huérfanos.
/// </remarks>
public static class IdentidadesDelSistema
{
    /// <summary>
    /// Obtiene el identificador de la cuenta de sistema.
    /// </summary>
    /// <value>
    /// Se usa al registrar veredictos del escaneo de malware y otras acciones
    /// automáticas que no origina una persona.
    /// </value>
    public static Guid Sistema { get; } = new("00000000-0000-0000-0000-000000000001");
}
