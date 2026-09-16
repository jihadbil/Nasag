namespace NasaqPackager.Services;

public interface IPackagerSettings
{
    string ProjectPath { get; set; }
    string ReleasesPath { get; set; }
    string IconPath { get; set; }
    string Channel { get; set; }
    string RuntimeIdentifier { get; set; }
    bool SelfContained { get; set; }
    string PackId { get; set; }
    string PackTitle { get; set; }
    bool RunObfuscar { get; set; }

    void Save();
    void Load();
}
