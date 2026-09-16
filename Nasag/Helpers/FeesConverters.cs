using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Nasag.Models;

namespace Nasag.Helpers;

public sealed class InstallmentStatusToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is InstallmentStatus s ? s switch
        {
            InstallmentStatus.Due => "مستحق",
            InstallmentStatus.Paid => "مدفوع",
            InstallmentStatus.PartiallyPaid => "مدفوع جزئياً",
            InstallmentStatus.Overdue => "متأخر",
            _ => string.Empty
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InstallmentStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is InstallmentStatus s ? s switch
        {
            InstallmentStatus.Paid => "SuccessSoftBrush",
            InstallmentStatus.PartiallyPaid => "WarningSoftBrush",
            InstallmentStatus.Overdue => "DangerSoftBrush",
            InstallmentStatus.Due => "InfoSoftBrush",
            _ => "BorderBrush"
        } : "BorderBrush";
        return Application.Current?.Resources[key] as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InstallmentStatusToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is InstallmentStatus s ? s switch
        {
            InstallmentStatus.Paid => "SuccessBrush",
            InstallmentStatus.PartiallyPaid => "WarningBrush",
            InstallmentStatus.Overdue => "DangerBrush",
            InstallmentStatus.Due => "InfoBrush",
            _ => "TextSecondaryBrush"
        } : "TextSecondaryBrush";
        return Application.Current?.Resources[key] as Brush ?? Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PaymentMethodToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is PaymentMethod m ? m switch
        {
            PaymentMethod.Cash => "نقدي",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            PaymentMethod.Card => "بطاقة",
            PaymentMethod.Cheque => "شيك",
            PaymentMethod.Other => "أخرى",
            _ => string.Empty
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class DecimalToCurrencyArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        decimal d;
        try
        {
            d = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return string.Empty;
        }
        return MoneyFormatter.Format(d);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InstallmentNotPaidToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is InstallmentStatus s && s == InstallmentStatus.Paid
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the first item of an <see cref="System.Collections.IEnumerable"/> or <c>null</c>
/// when the source is empty. Used for the "print last receipt" button which needs the
/// most recent payment row as its <c>CommandParameter</c>.
/// </summary>
public sealed class FirstItemConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.IEnumerable items)
        {
            foreach (var item in items) return item;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
