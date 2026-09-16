using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;

namespace Nasag.Services;

public interface IConnectionMonitor
{
    bool IsConnected { get; }
    string? LastErrorMessage { get; }
    event EventHandler? StateChanged;

    Task<bool> CheckAsync(CancellationToken ct = default);
    void ReportFailure(string message);
    void ReportSuccess();
}

/// <summary>
/// Tracks SQL Server reachability. Actual probing uses <see cref="NasaqDbContext.Database.CanConnectAsync"/>.
/// State mutations are dispatched to the UI thread so WPF bindings are not torn from background threads.
/// </summary>
public sealed partial class ConnectionMonitor : ObservableObject, IConnectionMonitor
{
    private readonly IDbContextFactory<NasaqDbContext>? _factory;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string? _lastErrorMessage;

    public event EventHandler? StateChanged;

    public ConnectionMonitor(IDbContextFactory<NasaqDbContext>? factory = null)
    {
        _factory = factory;
    }

    public async Task<bool> CheckAsync(CancellationToken ct = default)
    {
        if (_factory is null) return IsConnected;

        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var ok = await ctx.Database.CanConnectAsync(ct).ConfigureAwait(false);
            return ok;
        }
        catch (Exception ex)
        {
            DispatchSet(() => LastErrorMessage = ex.Message);
            return false;
        }
    }

    public void ReportFailure(string message)
    {
        DispatchSet(() =>
        {
            LastErrorMessage = message;
            if (IsConnected)
            {
                IsConnected = false;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public void ReportSuccess()
    {
        DispatchSet(() =>
        {
            LastErrorMessage = null;
            if (!IsConnected)
            {
                IsConnected = true;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private static void DispatchSet(Action action)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            app.Dispatcher.Invoke(action);
        }
    }
}
