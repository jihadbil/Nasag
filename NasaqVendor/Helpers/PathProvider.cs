using System;
using System.IO;

namespace NasaqVendor.Helpers;

/// <summary>
/// Centralized %LOCALAPPDATA%\NasaqVendor path resolver.
/// </summary>
public static class PathProvider
{
    public static string AppDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NasaqVendor");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DatabaseFile => Path.Combine(AppDir, "vendor.db");
    public static string IssuerPrivateKeyFile => Path.Combine(AppDir, "issuer.private.dpapi");
    public static string IssuerPublicKeyFile => Path.Combine(AppDir, "issuer.public.key");
    public static string SettingsFile => Path.Combine(AppDir, "settings.json");
}
