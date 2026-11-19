using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Catering.Desktop.Converters;

/// <summary>Ненулевое целое → true.</summary>
public sealed class NotZeroToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i ? i != 0 : value is long l ? l != 0 : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
