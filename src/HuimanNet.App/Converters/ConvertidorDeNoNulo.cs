using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Indica si un valor tiene contenido (no nulo y, si es texto, no vacío).</summary>
public sealed class ConvertidorDeNoNulo : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string texto ? !string.IsNullOrWhiteSpace(texto) : value is not null;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
