using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Nasag.Services;

namespace Nasag.Controls;

public partial class ToastHost : UserControl
{
    public ObservableCollection<ToastItem> Toasts { get; } = new();
    private IToastService? _service;

    public ToastHost()
    {
        InitializeComponent();
        ToastList.ItemsSource = Toasts;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.Host is null) return;
        try
        {
            _service = (IToastService?)App.Host.Services.GetService(typeof(IToastService));
            if (_service is not null)
            {
                _service.ToastAdded += OnToastAdded;
                _service.ToastRemoved += OnToastRemoved;
            }
        }
        catch
        {
            _service = null;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_service is not null)
        {
            _service.ToastAdded -= OnToastAdded;
            _service.ToastRemoved -= OnToastRemoved;
        }
    }

    private void OnToastAdded(object? sender, ToastItem item) => Toasts.Add(item);

    private void OnToastRemoved(object? sender, int id)
    {
        for (var i = Toasts.Count - 1; i >= 0; i--)
        {
            if (Toasts[i].Id == id) Toasts.RemoveAt(i);
        }
    }

    private void OnDismissClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is int id)
            _service?.Dismiss(id);
    }
}
