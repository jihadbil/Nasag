using System;
using System.Threading.Tasks;
using System.Windows;
using Nasag.Views.Common;

namespace Nasag.Services;

/// <summary>
/// Themed dialog service. Replaces the Windows-default MessageBox with the
/// branded <see cref="NasaqDialog"/> (rounded corners, Tajawal font, RTL layout).
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string okText = "تأكيد", string cancelText = "إلغاء")
        => InvokeAsync(() => NasaqDialog.Confirm(ResolveOwner(), title, message, okText, cancelText, NasaqDialogKind.Question));

    public Task<bool> ConfirmDestructiveAsync(string title, string message, string okText = "حذف", string cancelText = "إلغاء")
        => InvokeAsync(() => NasaqDialog.Confirm(ResolveOwner(), title, message, okText, cancelText, NasaqDialogKind.Danger));

    public Task ShowInfoAsync(string title, string message)
        => InvokeAsync<bool>(() =>
        {
            NasaqDialog.Show(ResolveOwner(), title, message, NasaqDialogKind.Info);
            return true;
        });

    public Task ShowSuccessAsync(string title, string message)
        => InvokeAsync<bool>(() =>
        {
            NasaqDialog.Show(ResolveOwner(), title, message, NasaqDialogKind.Success);
            return true;
        });

    public Task ShowWarningAsync(string title, string message)
        => InvokeAsync<bool>(() =>
        {
            NasaqDialog.Show(ResolveOwner(), title, message, NasaqDialogKind.Warning);
            return true;
        });

    public Task ShowErrorAsync(string title, string message)
        => InvokeAsync<bool>(() =>
        {
            NasaqDialog.Show(ResolveOwner(), title, message, NasaqDialogKind.Danger);
            return true;
        });

    private static Window? ResolveOwner()
    {
        var app = Application.Current;
        if (app is null) return null;
        // Prefer an active modal window so the dialog parents correctly.
        foreach (Window w in app.Windows)
        {
            if (w.IsActive) return w;
        }
        return app.MainWindow;
    }

    private static Task<T> InvokeAsync<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            return Task.FromResult(action());

        var tcs = new TaskCompletionSource<T>();
        dispatcher.BeginInvoke(new Action(() =>
        {
            try { tcs.SetResult(action()); }
            catch (Exception ex) { tcs.SetException(ex); }
        }));
        return tcs.Task;
    }
}
