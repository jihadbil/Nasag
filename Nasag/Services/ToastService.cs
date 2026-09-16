using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Nasag.Services;

public sealed class ToastService : IToastService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);
    private int _nextId;

    public event EventHandler<ToastItem>? ToastAdded;
    public event EventHandler<int>? ToastRemoved;

    public void Show(ToastKind kind, string title, string? message = null)
    {
        var id = Interlocked.Increment(ref _nextId);
        var item = new ToastItem(id, kind, title, message);

        OnUiThread(() => ToastAdded?.Invoke(this, item));
        _ = AutoDismissAsync(id, DefaultDuration);
    }

    public void Success(string title, string? message = null) => Show(ToastKind.Success, title, message);
    public void Error(string title, string? message = null) => Show(ToastKind.Error, title, message);
    public void Warning(string title, string? message = null) => Show(ToastKind.Warning, title, message);
    public void Info(string title, string? message = null) => Show(ToastKind.Info, title, message);

    public void Dismiss(int id) => OnUiThread(() => ToastRemoved?.Invoke(this, id));

    private async Task AutoDismissAsync(int id, TimeSpan after)
    {
        await Task.Delay(after).ConfigureAwait(false);
        Dismiss(id);
    }

    private static void OnUiThread(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null || d.CheckAccess()) action();
        else d.BeginInvoke(action);
    }
}
