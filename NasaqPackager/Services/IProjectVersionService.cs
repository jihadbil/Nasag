namespace NasaqPackager.Services;

public interface IProjectVersionService
{
    string? GetCurrentVersion(string csprojPath);
    string BumpPatch(string csprojPath);
    string SetVersion(string csprojPath, string newVersion);
}
