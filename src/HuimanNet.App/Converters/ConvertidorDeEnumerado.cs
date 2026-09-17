using System.Globalization;
using HuimanNet.App.Localizacion;

namespace HuimanNet.App.Converters;

/// <summary>Traduce un valor de enumeración con las claves <c>enum.Tipo.Valor</c>.</summary>
public sealed class ConvertidorDeEnumerado : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum valor
            ? Textos.Traductor.Traducir(string.Create(CultureInfo.InvariantCulture, $"enum.{valor.GetType().Name}.{valor}"))
            : string.Empty;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
