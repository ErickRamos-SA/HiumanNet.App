using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Formatea importes con dos decimales en la cultura del idioma elegido.</summary>
public sealed class ConvertidorDeMoneda : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal importe => importe.ToString("N2", Textos.Traductor.Cultura),
            int entero => entero.ToString("N0", Textos.Traductor.Cultura),
            _ => string.Empty,
        };

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
