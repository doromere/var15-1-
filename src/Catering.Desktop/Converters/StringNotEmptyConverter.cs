using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Catering.Desktop.Converters;

/// <summary>Строка непустая → true (для видимости сообщений об ошибках и подсказок).</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrWhiteSpace(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
