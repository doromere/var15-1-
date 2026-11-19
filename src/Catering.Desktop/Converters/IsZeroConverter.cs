using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Catering.Desktop.Converters;

/// <summary>Ноль → true (для показа пустых состояний).</summary>
public sealed class IsZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i ? i == 0 : value is long l ? l == 0 : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
