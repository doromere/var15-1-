using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Catering.Core.Common;

namespace Catering.Desktop.Converters;

/// <summary>Цвет подложки «чипа» статуса договора (цветовая индикация).</summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    public static readonly IBrush PreliminaryBrush = new SolidColorBrush(Color.Parse("#F3E9C8"));
    public static readonly IBrush ApprovedBrush = new SolidColorBrush(Color.Parse("#DDEAD9"));
    public static readonly IBrush ProcurementBrush = new SolidColorBrush(Color.Parse("#F6DFC4"));
    public static readonly IBrush CompletedBrush = new SolidColorBrush(Color.Parse("#CBDDD2"));
    public static readonly IBrush CancelledBrush = new SolidColorBrush(Color.Parse("#F2D2D2"));
    public static readonly IBrush FallbackBrush = new SolidColorBrush(Color.Parse("#EDE6D8"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            ContractStatuses.Preliminary => PreliminaryBrush,
            ContractStatuses.Approved => ApprovedBrush,
            ContractStatuses.Procurement => ProcurementBrush,
            ContractStatuses.Completed => CompletedBrush,
            ContractStatuses.Cancelled => CancelledBrush,
            _ => FallbackBrush,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
