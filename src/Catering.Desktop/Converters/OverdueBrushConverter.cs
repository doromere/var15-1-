using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Catering.Core.Common;

namespace Catering.Desktop.Converters;

/// <summary>MultiBinding [EventDatetime, Status] → цвет индикатора просрочки (терракота / приглушённый серый).</summary>
public sealed class OverdueBrushConverter : IMultiValueConverter
{
    private static readonly IBrush Critical = new SolidColorBrush(Color.Parse("#B4552D"));
    private static readonly IBrush Neutral = new SolidColorBrush(Color.Parse("#6B5E4E"));

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return Neutral;
        if (values[0] is not DateTime eventDate || values[1] is not string status) return Neutral;
        return eventDate < DateTime.Now && !ContractStatuses.IsTerminal(status)
            ? Critical
            : Neutral;
    }

    public object? ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
