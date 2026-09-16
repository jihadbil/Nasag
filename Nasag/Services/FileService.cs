using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Nasag.Services;

public sealed class FileService : IFileService
{
    public string? PickImage(Window? owner = null)
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر صورة الطالب",
            Filter = "صور (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|كل الملفات (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true,
        };

        // Default to the user's Pictures folder when it exists — avoids the dialog
        // opening in a directory the user can't navigate from.
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (!string.IsNullOrEmpty(pictures) && Directory.Exists(pictures))
            dlg.InitialDirectory = pictures;

        var resolvedOwner = owner ?? ResolveActiveOwner();
        var result = resolvedOwner is not null ? dlg.ShowDialog(resolvedOwner) : dlg.ShowDialog();
        return result == true ? dlg.FileName : null;
    }

    public async Task<byte[]?> ReadAllBytesAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
    }

    public bool CanDisplayImage(byte[]? bytes)
    {
        if (bytes is not { Length: > 0 }) return false;
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            var frame = BitmapFrame.Create(
                ms,
                BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            return frame.PixelWidth > 0 && frame.PixelHeight > 0;
        }
        catch
        {
            return false;
        }
    }

    private static Window? ResolveActiveOwner()
    {
        var app = Application.Current;
        if (app is null) return null;
        foreach (Window w in app.Windows)
            if (w.IsActive) return w;
        return app.MainWindow;
    }
}
