using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Formatea fechas e instantes en la cultura del idioma elegido.</summary>
public sealed class ConvertidorDeFecha : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            DateTimeOffset instante => instante.ToLocalTime().ToString("g", Textos.Traductor.Cultura),
            DateOnly fecha => fecha.ToString("d", Textos.Traductor.Cultura),
            _ => string.Empty,
        };

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
