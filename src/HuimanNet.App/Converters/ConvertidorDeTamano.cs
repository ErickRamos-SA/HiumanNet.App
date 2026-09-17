using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Muestra un tamaño en bytes con la unidad adecuada.</summary>
public sealed class ConvertidorDeTamano : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double bytes = value is long l ? l : value is int i ? i : 0;
        CultureInfo cultura = Textos.Traductor.Cultura;

        return bytes switch
        {
            < 1024 => $"{bytes.ToString("N0", cultura)} B",
            < 1024 * 1024 => $"{(bytes / 1024).ToString("N1", cultura)} KB",
            _ => $"{(bytes / (1024 * 1024)).ToString("N1", cultura)} MB",
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
