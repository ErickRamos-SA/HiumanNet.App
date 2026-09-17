using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Services;

/// <summary>
/// Navegación sobre <see cref="Shell"/>.
/// </summary>
public sealed class ServicioDeNavegacionShell : IServicioDeNavegacion
{
    /// <inheritdoc/>
    public Task IrAAsync(string ruta, IDictionary<string, object>? parametros = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruta);

        Shell shell = Shell.Current
            ?? throw new InvalidOperationException("No hay un Shell activo para navegar.");

        return parametros is null ? shell.GoToAsync(ruta) : shell.GoToAsync(ruta, parametros);
    }

    /// <inheritdoc/>
    public Task VolverAsync()
    {
        Shell shell = Shell.Current
            ?? throw new InvalidOperationException("No hay un Shell activo para navegar.");

        return shell.GoToAsync("..");
    }
}
