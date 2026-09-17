namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Administrador que se crea al arrancar cuando la base de datos no tiene ninguno.
/// </summary>
/// <remarks>
/// Sólo existe para poder entrar la primera vez. El usuario queda obligado a
/// cambiar la contraseña en su primer acceso.
/// </remarks>
public sealed class OpcionesDeAdministradorInicial
{
    /// <summary>Obtiene o establece el correo del administrador inicial.</summary>
    /// <value>Vacío para no crear ninguno.</value>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Obtiene o establece el nombre del administrador inicial.</summary>
    /// <value><c>Administrador</c> por defecto.</value>
    public string NombreCompleto { get; set; } = "Administrador";

    /// <summary>Obtiene o establece la contraseña inicial.</summary>
    /// <value>Debe resolverse desde secretos; nunca en <c>appsettings.json</c> de producción.</value>
    public string Contrasena { get; set; } = string.Empty;
}
