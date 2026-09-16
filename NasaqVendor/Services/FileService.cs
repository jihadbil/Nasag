using Microsoft.Win32;

namespace NasaqVendor.Services;

public sealed class FileService : IFileService
{
    public string? SaveFile(string title, string defaultFileName, string filter, string defaultExt)
    {
        var dlg = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter,
            DefaultExt = defaultExt,
            OverwritePrompt = true,
            AddExtension = true
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenFile(string title, string filter)
    {
        var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
