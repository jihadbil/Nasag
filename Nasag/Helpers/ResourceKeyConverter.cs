using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nasag.Helpers;

/// <summary>
/// Resolves a string resource key against Application.Current.Resources. Used to bind icon-key strings
/// (e.g. "IconDashboard") to the actual Geometry stored in Icons.xaml.
/// </summary>
public sealed class ResourceKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrEmpty(key))
            return null;
        return Application.Current.TryFindResource(key);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
