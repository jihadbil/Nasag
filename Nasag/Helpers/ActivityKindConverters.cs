using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Nasag.Services;

namespace Nasag.Helpers;

public sealed class ActivityKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var brushKey = value is ActivityKind k && k == ActivityKind.PaymentReceived
            ? "SuccessBrush"
            : "TealPrimaryBrush";
        return Application.Current?.TryFindResource(brushKey) ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ActivityKindToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var iconKey = value is ActivityKind k && k == ActivityKind.PaymentReceived
            ? "IconFees"
            : "IconStudents";
        return Application.Current?.TryFindResource(iconKey) ?? Geometry.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
