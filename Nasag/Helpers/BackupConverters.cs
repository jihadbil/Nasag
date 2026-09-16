using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Nasag.Models;

namespace Nasag.Helpers;

public sealed class BackupKindToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is BackupKind k ? k switch
        {
            BackupKind.Backup => "نسخة احتياطية",
            BackupKind.Restore => "استعادة",
            _ => string.Empty
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BackupKindToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is BackupKind k ? k switch
        {
            BackupKind.Backup => "TealSoftBrush",
            BackupKind.Restore => "WarningSoftBrush",
            _ => "BorderBrush"
        } : "BorderBrush";
        return Application.Current?.Resources[key] as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BackupKindToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is BackupKind k ? k switch
        {
            BackupKind.Backup => "TealPressedBrush",
            BackupKind.Restore => "WarningBrush",
            _ => "TextSecondaryBrush"
        } : "TextSecondaryBrush";
        return Application.Current?.Resources[key] as Brush ?? Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
