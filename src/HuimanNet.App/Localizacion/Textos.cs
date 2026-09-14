using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace HuimanNet.App.Localizacion;

/// <summary>
/// Aviso de que el usuario cambió el idioma de la interfaz.
/// </summary>
/// <param name="Idioma">Idioma nuevo.</param>
public sealed record IdiomaCambiadoMensaje(Idioma Idioma);

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

/// <summary>
/// Texto traducido observable: notifica cuando cambia el idioma.
/// </summary>
public sealed class TextoLocalizado : ObservableObject
{
    private readonly string _clave;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TextoLocalizado"/>.
    /// </summary>
    /// <param name="clave">Clave del texto.</param>
    public TextoLocalizado(string clave)
    {
        _clave = clave;

        // Registro débil: el texto no impide que la página se libere.
        WeakReferenceMessenger.Default.Register<TextoLocalizado, IdiomaCambiadoMensaje>(
            this, static (receptor, _) => receptor.OnPropertyChanged(nameof(Valor)));
    }

    /// <summary>Obtiene el texto en el idioma actual.</summary>
    /// <value>La traducción, o la clave si no existe.</value>
    public string Valor => Textos.Traductor[_clave];
}

/// <summary>
/// Extensión de marcado <c>{loc:T Clave=...}</c> para textos fijos en XAML.
/// </summary>
/// <remarks>
/// Devuelve un enlace tipado (sin reflexión, compatible con el recorte de la
/// publicación) a un <see cref="TextoLocalizado"/>, así el texto cambia en
/// cuanto el usuario elige otro idioma, sin recrear la página.
/// </remarks>
[ContentProperty(nameof(Clave))]
[AcceptEmptyServiceProvider]
public sealed class TExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Obtiene o establece la clave del texto.</summary>
    /// <value>Por ejemplo <c>inicio.titulo</c>.</value>
    public string Clave { get; set; } = string.Empty;

    /// <inheritdoc/>
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        => new TypedBinding<TextoLocalizado, string>(
            static texto => (texto.Valor, true),
            null!,
            [Tuple.Create<Func<TextoLocalizado, object>, string>(static texto => texto, nameof(TextoLocalizado.Valor))])
        {
            Mode = BindingMode.OneWay,
            Source = new TextoLocalizado(Clave),
        };

    /// <inheritdoc/>
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
