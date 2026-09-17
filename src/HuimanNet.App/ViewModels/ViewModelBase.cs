using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Base de las ViewModel: estado de ocupado, mensajes y traducción.
/// </summary>
/// <remarks>
/// Centraliza el manejo de errores para que ninguna pantalla muestre una
/// excepción cruda: los rechazos de la API llegan con el mensaje que redactó
/// el servidor y los problemas de red con un texto traducido.
/// </remarks>
public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ViewModelBase"/>.
    /// </summary>
    /// <param name="traductor">Traductor de la aplicación.</param>
    protected ViewModelBase(Traductor traductor)
    {
        T = traductor;

        WeakReferenceMessenger.Default.Register<ViewModelBase, IdiomaCambiadoMensaje>(
            this, static (receptor, _) => receptor.AlCambiarIdiomaInterno());
    }

    /// <summary>Obtiene el traductor, para los textos que se arman en código.</summary>
    /// <value>Instancia compartida.</value>
    public Traductor T { get; }

    /// <summary>Obtiene o establece si hay una operación en curso.</summary>
    /// <value><c>true</c> mientras se espera a la API.</value>
    [ObservableProperty]
    public partial bool EstaOcupado { get; set; }

    /// <summary>Obtiene o establece el mensaje de error de la última operación.</summary>
    /// <value><c>null</c> si terminó bien.</value>
    [ObservableProperty]
    public partial string? MensajeDeError { get; set; }

    /// <summary>Obtiene o establece el mensaje de éxito de la última operación.</summary>
    /// <value><c>null</c> si no hay nada que confirmar.</value>
    [ObservableProperty]
    public partial string? MensajeDeExito { get; set; }

    /// <summary>Obtiene o establece el título de la pantalla.</summary>
    /// <value>Texto ya traducido.</value>
    [ObservableProperty]
    public partial string Titulo { get; set; } = "HuimanNet";

    /// <summary>Indica si hay un error que mostrar.</summary>
    /// <value><c>true</c> si <see cref="MensajeDeError"/> tiene texto.</value>
    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeDeError);

    /// <summary>Indica si hay una confirmación que mostrar.</summary>
    /// <value><c>true</c> si <see cref="MensajeDeExito"/> tiene texto.</value>
    public bool TieneExito => !string.IsNullOrWhiteSpace(MensajeDeExito);

    /// <summary>
    /// Ejecuta una operación con indicador de ocupado y traducción de errores.
    /// </summary>
    /// <param name="operacion">Operación a ejecutar.</param>
    /// <returns><c>true</c> si terminó sin errores.</returns>
    protected async Task<bool> EjecutarAsync(Func<Task> operacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (EstaOcupado)
        {
            return false;
        }

        EstaOcupado = true;
        MensajeDeError = null;

        try
        {
            await operacion();
            return true;
        }
        catch (ErrorDeApiException excepcion)
        {
            MensajeDeError = excepcion.Message;
        }
        catch (HttpRequestException excepcion)
        {
            MensajeDeError = T["movil.sinConexion"];
            System.Diagnostics.Debug.WriteLine(excepcion);
        }
        catch (OperationCanceledException)
        {
            // Incluye TaskCanceledException (HttpClient) y las cancelaciones de la
            // política de resiliencia; sin esto, una espera agotada cerraría la app.
            MensajeDeError = T["movil.tiempoAgotado"];
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeDeError = excepcion.Message;
        }
        finally
        {
            EstaOcupado = false;
        }

        return false;
    }

    /// <summary>
    /// Sustituye el contenido de una colección observable.
    /// </summary>
    /// <typeparam name="TElemento">Tipo de los elementos.</typeparam>
    /// <param name="destino">Colección enlazada a la vista.</param>
    /// <param name="origen">Elementos nuevos.</param>
    protected static void Reemplazar<TElemento>(ObservableCollection<TElemento> destino, IEnumerable<TElemento> origen)
    {
        ArgumentNullException.ThrowIfNull(destino);
        ArgumentNullException.ThrowIfNull(origen);

        destino.Clear();

        foreach (TElemento elemento in origen)
        {
            destino.Add(elemento);
        }
    }

    /// <summary>
    /// Punto de extensión para rehacer los textos armados en código tras un cambio de idioma.
    /// </summary>
    protected virtual void AlCambiarIdioma()
    {
    }

    /// <summary>Notifica <see cref="TieneError"/> al cambiar el mensaje de error.</summary>
    /// <param name="value">Mensaje nuevo.</param>
    partial void OnMensajeDeErrorChanged(string? value) => OnPropertyChanged(nameof(TieneError));

    /// <summary>Notifica <see cref="TieneExito"/> al cambiar el mensaje de éxito.</summary>
    /// <param name="value">Mensaje nuevo.</param>
    partial void OnMensajeDeExitoChanged(string? value) => OnPropertyChanged(nameof(TieneExito));

    /// <summary>Refresca el traductor enlazado y los textos armados en código tras un cambio de idioma.</summary>
    private void AlCambiarIdiomaInterno()
    {
        OnPropertyChanged(nameof(T));
        AlCambiarIdioma();
    }
}
