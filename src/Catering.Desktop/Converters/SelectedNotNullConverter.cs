using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Catering.Desktop.Converters;

/// <summary>Объект не null → true (для IsEnabled у кнопок, работающих с выбранной строкой).</summary>
public sealed class SelectedNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
