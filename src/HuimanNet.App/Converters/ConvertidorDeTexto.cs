using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Traduce una clave de texto que llega como dato (por ejemplo, los pendientes del inicio).</summary>
public sealed class ConvertidorDeTexto : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string clave ? Textos.Traductor[clave] : string.Empty;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
