namespace HuimanNet.App.Services;

/// <summary>
/// Navegación entre pantallas, desacoplada de Shell para poder probar las ViewModel.
/// </summary>
public interface IServicioDeNavegacion
{
    /// <summary>
    /// Navega a una ruta.
    /// </summary>
    /// <param name="ruta">Ruta absoluta (<c>//portal/inicio</c>) o relativa (<c>corrida</c>).</param>
    /// <param name="parametros">Parámetros para la página de destino, o <c>null</c>.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    Task IrAAsync(string ruta, IDictionary<string, object>? parametros = null);

    /// <summary>
    /// Vuelve a la pantalla anterior.
    /// </summary>
    /// <returns>Una tarea que representa la navegación.</returns>
    Task VolverAsync();
}
