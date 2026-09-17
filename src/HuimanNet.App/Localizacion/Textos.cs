using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace HuimanNet.App.Localizacion;

/// <summary>
/// Punto de acceso al traductor compartido por XAML, convertidores y ViewModel.
/// </summary>
/// <remarks>
/// Usa los mismos textos que la web (recursos incrustados en
/// <c>HuimanNet.Contracts</c>), de modo que las dos interfaces dicen lo mismo.
/// El idioma elegido se recuerda en las preferencias del dispositivo y se
/// sincroniza con el perfil del usuario al iniciar sesión.
/// </remarks>
public static class Textos
{
    /// <summary>Clave de las preferencias del dispositivo donde se guarda el idioma.</summary>
    private const string ClavePreferencia = "huimannet.idioma";

    private static readonly Lazy<Traductor> Instancia = new(() => new Traductor(LeerPreferencia()));

    /// <summary>Obtiene el traductor de la aplicación.</summary>
    /// <value>Instancia única, creada al primer uso.</value>
    public static Traductor Traductor => Instancia.Value;

    /// <summary>
    /// Cambia el idioma de toda la interfaz y lo recuerda en el dispositivo.
    /// </summary>
    /// <param name="idioma">Idioma a aplicar.</param>
    public static void Aplicar(Idioma idioma)
    {
        Traductor traductor = Traductor;
        bool cambio = traductor.Idioma != idioma;

        traductor.Cambiar(idioma);
        Preferences.Default.Set(ClavePreferencia, idioma.Codigo());
        CultureInfo.CurrentCulture = traductor.Cultura;
        CultureInfo.CurrentUICulture = traductor.Cultura;

        if (cambio)
        {
            WeakReferenceMessenger.Default.Send(new IdiomaCambiadoMensaje(traductor.Idioma));
        }
    }

    /// <summary>Lee el idioma recordado en el dispositivo.</summary>
    /// <returns>El idioma guardado; si no hay, el del sistema (inglés o, en otro caso, español).</returns>
    private static Idioma LeerPreferencia()
    {
        string? codigo = Preferences.Default.Get<string?>(ClavePreferencia, null);

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            return IdiomaExtensiones.DesdeCodigo(codigo);
        }

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? Idioma.Ingles : Idioma.Espanol;
    }
}
