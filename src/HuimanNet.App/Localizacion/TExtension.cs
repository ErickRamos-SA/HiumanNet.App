using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace HuimanNet.App.Localizacion;

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
