using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Catering.Desktop.Converters;

/// <summary>Цвет текста статуса: критические/закрытые — тёмно-красный/зелёный, прочие — нейтральные.</summary>
public sealed class StatusToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            Catering.Core.Common.ContractStatuses.Cancelled => new SolidColorBrush(Color.Parse("#8C2F2F")),
            Catering.Core.Common.ContractStatuses.Completed => new SolidColorBrush(Color.Parse("#173E25")),
            Catering.Core.Common.ContractStatuses.Procurement => new SolidColorBrush(Color.Parse("#5B2E07")),
            Catering.Core.Common.ContractStatuses.Approved => new SolidColorBrush(Color.Parse("#173E25")),
            Catering.Core.Common.ContractStatuses.Preliminary => new SolidColorBrush(Color.Parse("#5C4A00")),
            _ => new SolidColorBrush(Color.Parse("#2A2118")),
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
