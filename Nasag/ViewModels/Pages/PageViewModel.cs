using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nasag.ViewModels.Pages;

public abstract partial class PageViewModel : ObservableObject
{
    public abstract string TitleAr { get; }
    public virtual string SubtitleAr => string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Called by the shell whenever the page becomes active. Override to load data.
    /// </summary>
    public virtual Task ActivateAsync(CancellationToken ct = default) => Task.CompletedTask;
}

// Phase 12: SettingsViewModel, UsersViewModel, and BackupViewModel have moved to
// their own dedicated namespaces (Nasag.ViewModels.Pages.Settings/Users/Backup)
// to allow full editor screens. Their stubs were removed from this file.
