namespace NasaqVendor.Services;

public interface IFileService
{
    /// <summary>Show SaveFileDialog. Returns chosen path or null.</summary>
    string? SaveFile(string title, string defaultFileName, string filter, string defaultExt);

    /// <summary>Show OpenFileDialog. Returns chosen path or null.</summary>
    string? OpenFile(string title, string filter);
}
