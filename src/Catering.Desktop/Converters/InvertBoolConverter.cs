using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Catering.Desktop.Converters;

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? false : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? false : true;
}
