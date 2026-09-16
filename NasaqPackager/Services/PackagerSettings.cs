using System;
using System.IO;
using System.Text.Json;

namespace NasaqPackager.Services;

public sealed class PackagerSettings : IPackagerSettings
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NasaqPackager");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private sealed class SettingsDto
    {
        public string ProjectPath { get; set; } = "";
        public string ReleasesPath { get; set; } = "";
        public string IconPath { get; set; } = "";
        public string Channel { get; set; } = "win";
        public string RuntimeIdentifier { get; set; } = "win-x64";
        public bool SelfContained { get; set; } = true;
        public string PackId { get; set; } = "Nasaq";
        public string PackTitle { get; set; } = "نَسَق لإدارة المدارس";
        public bool RunObfuscar { get; set; } = false;
    }

    public string ProjectPath { get; set; } = "";
    public string ReleasesPath { get; set; } = "";
    public string IconPath { get; set; } = "";
    public string Channel { get; set; } = "win";
    public string RuntimeIdentifier { get; set; } = "win-x64";
    public bool SelfContained { get; set; } = true;
    public string PackId { get; set; } = "Nasaq";
    public string PackTitle { get; set; } = "نَسَق لإدارة المدارس";
    public bool RunObfuscar { get; set; } = false;

    public PackagerSettings()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var dto = JsonSerializer.Deserialize<SettingsDto>(json);
                if (dto is not null)
                {
                    ProjectPath = dto.ProjectPath;
                    ReleasesPath = dto.ReleasesPath;
                    IconPath = dto.IconPath;
                    Channel = string.IsNullOrWhiteSpace(dto.Channel) ? "win" : dto.Channel;
                    RuntimeIdentifier = string.IsNullOrWhiteSpace(dto.RuntimeIdentifier) ? "win-x64" : dto.RuntimeIdentifier;
                    SelfContained = dto.SelfContained;
                    PackId = string.IsNullOrWhiteSpace(dto.PackId) ? "Nasaq" : dto.PackId;
                    PackTitle = string.IsNullOrWhiteSpace(dto.PackTitle) ? "نَسَق لإدارة المدارس" : dto.PackTitle;
                    RunObfuscar = dto.RunObfuscar;
                }
            }
        }
        catch
        {
            // ignore corrupted settings; fall back to defaults below
        }

        // Apply defaults if still empty — resolve relative to the running exe by walking up
        // until we find Nasag.slnx (developer machine convention).
        if (string.IsNullOrWhiteSpace(ProjectPath) ||
            string.IsNullOrWhiteSpace(ReleasesPath))
        {
            var solutionRoot = FindSolutionRoot();
            if (solutionRoot is not null)
            {
                if (string.IsNullOrWhiteSpace(ProjectPath))
                    ProjectPath = Path.Combine(solutionRoot, "Nasag", "Nasag.csproj");
                if (string.IsNullOrWhiteSpace(ReleasesPath))
                    ReleasesPath = Path.Combine(solutionRoot, "Releases", "Customer");
                if (string.IsNullOrWhiteSpace(IconPath))
                {
                    var ico = Path.Combine(solutionRoot, "Logo.ico");
                    if (File.Exists(ico)) IconPath = ico;
                }
            }
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var dto = new SettingsDto
            {
                ProjectPath = ProjectPath,
                ReleasesPath = ReleasesPath,
                IconPath = IconPath,
                Channel = Channel,
                RuntimeIdentifier = RuntimeIdentifier,
                SelfContained = SelfContained,
                PackId = PackId,
                PackTitle = PackTitle,
                RunObfuscar = RunObfuscar,
            };
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // best-effort persistence
        }
    }

    private static string? FindSolutionRoot()
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir is not null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Nasag.slnx")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        catch { }
        return null;
    }
}
