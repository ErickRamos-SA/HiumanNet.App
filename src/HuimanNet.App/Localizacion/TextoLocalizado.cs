using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace HuimanNet.App.Localizacion;

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
