using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Catering.Core.Common;

namespace Catering.Desktop.Converters;

/// <summary>
/// MultiBinding [EventDatetime, Status] → "ПРОСРОЧЕНО" при критическом состоянии
/// (дата выезда в прошлом, документ не закрыт), иначе пусто.
/// </summary>
public sealed class OverdueTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return string.Empty;
        if (values[0] is not DateTime eventDate || values[1] is not string status) return string.Empty;
        return eventDate < DateTime.Now && !ContractStatuses.IsTerminal(status)
            ? "ПРОСРОЧЕНО"
            : string.Empty;
    }

    public object? ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
