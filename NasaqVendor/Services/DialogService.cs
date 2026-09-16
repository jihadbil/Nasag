using System.Windows;
using NasaqVendor.Views.Common;

namespace NasaqVendor.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message, string okText = "تأكيد", string cancelText = "إلغاء", DialogKind kind = DialogKind.Question)
    {
        var owner = Application.Current?.MainWindow;
        return VendorDialog.Confirm(owner, title, message, okText, cancelText, kind);
    }

    public void Info(string title, string message, DialogKind kind = DialogKind.Info)
    {
        var owner = Application.Current?.MainWindow;
        VendorDialog.Show(owner, title, message, kind);
    }
}
