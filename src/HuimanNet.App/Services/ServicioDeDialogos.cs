using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Services;

/// <summary>
/// Diálogos nativos sobre la página visible, con los botones traducidos.
/// </summary>
public sealed class ServicioDeDialogos : IServicioDeDialogos
{
    /// <inheritdoc/>
    public Task MostrarAvisoAsync(string titulo, string mensaje)
    {
        Page? pagina = ObtenerPaginaActual();

        return pagina is null
            ? Task.CompletedTask
            : pagina.DisplayAlertAsync(titulo, mensaje, Textos.Traductor["movil.aceptar"]);
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar)
    {
        Page? pagina = ObtenerPaginaActual();

        return pagina is not null
            && await pagina.DisplayAlertAsync(titulo, mensaje, aceptar, Textos.Traductor["comun.cancelar"]);
    }

    /// <inheritdoc/>
    public async Task<string?> ElegirAsync(string titulo, params string[] opciones)
    {
        Page? pagina = ObtenerPaginaActual();

        if (pagina is null)
        {
            return null;
        }

        string cancelar = Textos.Traductor["comun.cancelar"];
        string resultado = await pagina.DisplayActionSheetAsync(titulo, cancelar, null, opciones);

        return resultado is null || resultado == cancelar ? null : resultado;
    }

    /// <summary>Obtiene la página visible sobre la que se muestran los diálogos.</summary>
    /// <returns>La página actual del shell o de la ventana principal; <c>null</c> si aún no hay ninguna.</returns>
    private static Page? ObtenerPaginaActual()
        => Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
}
