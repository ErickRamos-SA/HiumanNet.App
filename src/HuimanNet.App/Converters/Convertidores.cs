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
