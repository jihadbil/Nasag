using System;
using System.Globalization;
using System.Windows.Data;

namespace Nasag.Helpers;

/// <summary>
/// Formats a byte count as a human-readable Arabic file size (ك.ب / م.ب / غ.ب).
/// Declared as a XAML resource inside <c>BackupView</c> — not registered globally
/// because no other screen exposes file sizes yet.
/// </summary>
public sealed class BytesToFileSizeConverter : IValueConverter
{
    private const double KB = 1024d;
    private const double MB = 1024d * 1024d;
    private const double GB = 1024d * 1024d * 1024d;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes;
        try { bytes = System.Convert.ToInt64(value, CultureInfo.InvariantCulture); }
        catch { return string.Empty; }

        if (bytes <= 0) return "—";
        if (bytes < KB) return $"{bytes} بايت";
        if (bytes < MB) return string.Format(CultureInfo.InvariantCulture, "{0:0.##} ك.ب", bytes / KB);
        if (bytes < GB) return string.Format(CultureInfo.InvariantCulture, "{0:0.##} م.ب", bytes / MB);
        return string.Format(CultureInfo.InvariantCulture, "{0:0.##} غ.ب", bytes / GB);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
