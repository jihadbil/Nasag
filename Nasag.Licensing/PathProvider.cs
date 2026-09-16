using System;
using System.IO;

namespace Nasag.Licensing;

/// <summary>
/// مسارات معروفة لمكونات الترخيص تحت %LOCALAPPDATA%\Nasaq.
/// </summary>
public static class PathProvider
{
    public static string NasaqLocalAppData { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nasaq");

    public static string StateFile { get; } =
        Path.Combine(NasaqLocalAppData, "state.dat");

    public static string LicenseFile { get; } =
        Path.Combine(NasaqLocalAppData, "license.naslic");

    public static string RegistrySubKey { get; } = @"Software\Nasaq\State";

    public static void EnsureNasaqFolderExists()
    {
        try
        {
            if (!Directory.Exists(NasaqLocalAppData))
                Directory.CreateDirectory(NasaqLocalAppData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"تعذّر إنشاء مجلد بيانات نسق المحلي: {NasaqLocalAppData}", ex);
        }
    }
}
