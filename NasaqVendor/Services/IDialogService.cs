namespace NasaqVendor.Services;

public enum DialogKind { Info, Success, Warning, Danger, Question }

public interface IDialogService
{
    bool Confirm(string title, string message, string okText = "تأكيد", string cancelText = "إلغاء", DialogKind kind = DialogKind.Question);
    void Info(string title, string message, DialogKind kind = DialogKind.Info);
}
