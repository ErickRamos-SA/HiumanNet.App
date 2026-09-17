namespace HuimanNet.App.Services;

/// <summary>
/// Diálogos del sistema, desacoplados de las páginas.
/// </summary>
public interface IServicioDeDialogos
{
    /// <summary>
    /// Muestra un aviso con un solo botón.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="mensaje">Mensaje.</param>
    /// <returns>Una tarea que termina al cerrar el aviso.</returns>
    Task MostrarAvisoAsync(string titulo, string mensaje);

    /// <summary>
    /// Pide confirmación antes de una acción.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="mensaje">Mensaje.</param>
    /// <param name="aceptar">Texto del botón que confirma.</param>
    /// <returns><c>true</c> si el usuario confirmó.</returns>
    Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar);

    /// <summary>
    /// Ofrece una lista de opciones.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="opciones">Opciones visibles.</param>
    /// <returns>La opción elegida, o <c>null</c> si canceló.</returns>
    Task<string?> ElegirAsync(string titulo, params string[] opciones);
}
