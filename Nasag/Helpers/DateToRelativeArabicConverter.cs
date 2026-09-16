using System;
using System.Globalization;
using System.Windows.Data;

namespace Nasag.Helpers;

public sealed class DateToRelativeArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dt) return string.Empty;

        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var diff = DateTime.Now - local;

        if (diff.TotalSeconds < 60) return "الآن";
        if (diff.TotalMinutes < 60)
        {
            var m = (int)diff.TotalMinutes;
            return m == 1 ? "قبل دقيقة" : m == 2 ? "قبل دقيقتين" : $"قبل {m} دقيقة";
        }
        if (diff.TotalHours < 24)
        {
            var h = (int)diff.TotalHours;
            return h == 1 ? "قبل ساعة" : h == 2 ? "قبل ساعتين" : $"قبل {h} ساعات";
        }
        if (diff.TotalDays < 7)
        {
            var d = (int)diff.TotalDays;
            return d == 1 ? "أمس" : d == 2 ? "قبل يومين" : $"قبل {d} أيام";
        }
        return local.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
