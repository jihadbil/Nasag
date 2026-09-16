using System;
using System.IO;
using System.Text.Json;

namespace Nasag.Services;

/// <summary>
/// Per-machine user preferences (Remember Me username, sort/page-size choices).
/// Stored as a tiny JSON file under %LOCALAPPDATA%\Nasaq\prefs.json.
/// These are UI preferences, NOT backup-able school data — they intentionally
/// live outside the SQL Server database.
/// </summary>
public sealed class UserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private readonly string _filePath;
    private UserPreferences _current = new();

    public UserPreferencesService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Nasaq");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "prefs.json");
        Reload();
    }

    public UserPreferences Current => _current;

    public void Reload()
    {
        try
        {
            if (!File.Exists(_filePath)) { _current = new UserPreferences(); return; }
            var json = File.ReadAllText(_filePath);
            _current = JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        catch
        {
            _current = new UserPreferences();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_current, JsonOpts);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Preferences are best-effort — never crash the app over a write failure.
        }
    }
}
