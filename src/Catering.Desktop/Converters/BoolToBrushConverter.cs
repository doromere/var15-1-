using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Catering.Desktop.Converters;

/// <summary>true → терракотовый (критическое состояние просрочки), false → приглушённый серый.</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    private static readonly IBrush Critical = new SolidColorBrush(Color.Parse("#B4552D"));
    private static readonly IBrush Normal = new SolidColorBrush(Color.Parse("#6B5E4E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Critical : Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
