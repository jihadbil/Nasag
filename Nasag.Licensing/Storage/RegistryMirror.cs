using System;
using Microsoft.Win32;

namespace Nasag.Licensing.Storage;

/// <summary>
/// مرآة في HKCU لمزامنة قيم بسيطة (نصوص / Ticks) — حماية إضافية من حذف ملف DPAPI.
/// </summary>
public sealed class RegistryMirror
{
    private readonly string _subKey;
    private readonly object _gate = new();

    public RegistryMirror(string subKey)
    {
        if (string.IsNullOrWhiteSpace(subKey))
            throw new ArgumentException("مسار مفتاح السجل مطلوب.", nameof(subKey));
        _subKey = subKey;
    }

    public string? GetString(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        lock (_gate)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(_subKey, writable: false);
                return key?.GetValue(name) as string;
            }
            catch
            {
                return null;
            }
        }
    }

    public void SetString(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("اسم القيمة مطلوب.", nameof(name));
        lock (_gate)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(_subKey, writable: true);
                key?.SetValue(name, value ?? "", RegistryValueKind.String);
            }
            catch
            {
                // فشل صامت — السجل ليس مصدر الحقيقة، فقط مرآة.
            }
        }
    }

    public long? GetTicks(string name)
    {
        var s = GetString(name);
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ticks))
            return ticks;
        return null;
    }

    public void SetTicks(string name, long ticks)
    {
        SetString(name, ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void DeleteValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        lock (_gate)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(_subKey, writable: true);
                key?.DeleteValue(name, throwOnMissingValue: false);
            }
            catch
            {
                // تجاهل
            }
        }
    }
}
