using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Invierte un valor lógico (por ejemplo, habilitar un botón mientras no hay trabajo en curso).</summary>
public sealed class ConvertidorDeNegacion : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
