using System;
using System.Windows;
using NasaqVendor.Views.Common;

namespace NasaqVendor.Services;

/// <summary>
/// Stores the active ToastHost and forwards Show() calls to it. The host is registered
/// by MainWindow during Loaded.
/// </summary>
public sealed class ToastService : IToastService
{
    private ToastHost? _host;

    public void Register(ToastHost host) => _host = host;

    public void Show(string message, ToastKind kind = ToastKind.Info)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (Application.Current is null) return;

        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            _host?.ShowToast(message, kind);
        }));
    }
}
