using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Licensing.License;
using Nasag.Models;
using Nasag.Services;
using Nasag.Services.Licensing;
using Nasag.ViewModels.Pages;

namespace Nasag.ViewModels.Shell;

public partial class MainShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IConnectionMonitor _connection;
    private readonly IAppInfoService _appInfo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILicenseService _license;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private NavigationItem? _activeItem;

    [ObservableProperty]
    private bool _isDisconnected;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private string _connectionErrorMessage = "تعذّر الاتصال بقاعدة البيانات. يرجى التحقق من الخادم.";

    [ObservableProperty]
    private string _schoolName = "مدرسة النور الأهلية";

    [ObservableProperty]
    private string _academicYear = "2025 - 2026";

    public string AppName => _appInfo.AppNameAr;
    public string AppTagline => _appInfo.AppTagline;
    public double SidebarWidth => IsSidebarCollapsed ? 78 : 260;
    public string SidebarToggleTooltip => IsSidebarCollapsed ? "توسيع القائمة" : "طي القائمة";

    public string UserDisplayName => _currentUser.DisplayName;
    public string UserInitial => _currentUser.Initial;
    public string? UserRoleName => _currentUser.User?.Role?.NameAr;

    [ObservableProperty] private string _licenseBadgeText = "—";
    [ObservableProperty] private string _licenseBadgeKind = "Info"; // Trial | Activated | Warning | Danger

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainShellViewModel(
        INavigationService navigation,
        IConnectionMonitor connection,
        IAppInfoService appInfo,
        ICurrentUserService currentUser,
        ILicenseService license)
    {
        _navigation = navigation;
        _connection = connection;
        _appInfo = appInfo;
        _currentUser = currentUser;
        _license = license;
        _license.StatusChanged += (_, _) => RefreshLicenseBadge();
        RefreshLicenseBadge();

        RebuildNavigationItems();

        _navigation.CurrentChanged += (_, _) => RefreshFromNavigation();
        _connection.StateChanged += (_, _) => SyncConnectionState();
        _currentUser.SignedIn += (_, _) => OnUserChanged();
        _currentUser.SignedOut += (_, _) => OnUserChanged();
        SyncConnectionState();

        _navigation.NavigateTo(NavigationSection.Dashboard);
    }

    /// <summary>
    /// Rebuilds the sidebar items so only sections the current user has
    /// permission for are visible. When no user is signed in, only items
    /// without a RequiredPermission are shown.
    /// </summary>
    private void RebuildNavigationItems()
    {
        NavigationItems.Clear();
        foreach (var d in _navigation.Descriptors)
        {
            if (!UserCanAccess(d)) continue;
            NavigationItems.Add(new NavigationItem(d, NavigateCommand));
        }

        // Reflect the currently active section on the freshly built items.
        foreach (var item in NavigationItems)
            item.IsActive = item.Section == _navigation.Current;
        ActiveItem = NavigationItems.FirstOrDefault(x => x.IsActive);
    }

    private bool UserCanAccess(NavigationDescriptor descriptor)
    {
        if (descriptor.RequiredPermission is not { } required) return true;
        return _currentUser.HasPermission(required);
    }

    [RelayCommand]
    private void Navigate(NavigationItem? item)
    {
        if (item is null) return;
        _navigation.NavigateTo(item.Section);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    [RelayCommand]
    private async Task RetryConnectionAsync()
    {
        var ok = await _connection.CheckAsync();
        if (ok)
            _connection.ReportSuccess();
        else
            _connection.ReportFailure(_connection.LastErrorMessage ?? "تعذّر الاتصال بقاعدة البيانات.");
    }

    [RelayCommand]
    private void Logout()
    {
        _currentUser.SignOut();
    }

    partial void OnIsSidebarCollapsedChanged(bool value)
    {
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(SidebarToggleTooltip));
    }

    private void OnUserChanged()
    {
        OnPropertyChanged(nameof(UserDisplayName));
        OnPropertyChanged(nameof(UserInitial));
        OnPropertyChanged(nameof(UserRoleName));

        // Re-filter the sidebar against the new identity (login/logout/role change).
        RebuildNavigationItems();

        // Reset to Dashboard when a new session begins so the shell never reopens deep-linked.
        // Also redirect when the user no longer has permission for the section they were on.
        if (_currentUser.IsAuthenticated)
        {
            var currentDescriptor = _navigation.Descriptors.FirstOrDefault(d => d.Section == _navigation.Current);
            if (currentDescriptor is null || !UserCanAccess(currentDescriptor))
                _navigation.NavigateTo(NavigationSection.Dashboard);
        }
    }

    private void RefreshFromNavigation()
    {
        CurrentPage = _navigation.CurrentViewModel;
        foreach (var item in NavigationItems)
            item.IsActive = item.Section == _navigation.Current;
        ActiveItem = NavigationItems.FirstOrDefault(x => x.IsActive);

        if (CurrentPage is PageViewModel page)
            _ = page.ActivateAsync();
    }

    private void SyncConnectionState()
    {
        IsDisconnected = !_connection.IsConnected;
        if (_connection.LastErrorMessage is { Length: > 0 } msg)
            ConnectionErrorMessage = msg;
    }

    private void RefreshLicenseBadge()
    {
        var status = _license.Status;
        switch (status)
        {
            case LicenseStatus.Trial t:
                LicenseBadgeText = $"تجربة — {t.DaysRemaining} يوم";
                LicenseBadgeKind = t.DaysRemaining <= 5 ? "Warning" : "Trial";
                break;
            case LicenseStatus.Activated act:
                var name = act.License?.CustomerName;
                LicenseBadgeText = string.IsNullOrWhiteSpace(name) ? "مفعَّل" : $"مفعَّل — {name}";
                LicenseBadgeKind = "Activated";
                break;
            case LicenseStatus.Expired:
                LicenseBadgeText = "منتهية الصلاحية";
                LicenseBadgeKind = "Danger";
                break;
            case LicenseStatus.TamperedClock:
                LicenseBadgeText = "ساعة النظام";
                LicenseBadgeKind = "Danger";
                break;
            case LicenseStatus.MachineMismatch:
                LicenseBadgeText = "جهاز مختلف";
                LicenseBadgeKind = "Danger";
                break;
            case LicenseStatus.InvalidSignature:
                LicenseBadgeText = "توقيع غير صحيح";
                LicenseBadgeKind = "Danger";
                break;
            case LicenseStatus.Missing:
                LicenseBadgeText = "غير مفعَّل";
                LicenseBadgeKind = "Warning";
                break;
            default:
                LicenseBadgeText = "—";
                LicenseBadgeKind = "Info";
                break;
        }
    }
}

public partial class NavigationItem : ObservableObject
{
    public NavigationSection Section { get; }
    public string TitleAr { get; }
    public string IconKey { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand<NavigationItem> Command { get; }

    [ObservableProperty]
    private bool _isActive;

    public NavigationItem(NavigationDescriptor descriptor, CommunityToolkit.Mvvm.Input.IRelayCommand<NavigationItem> command)
    {
        Section = descriptor.Section;
        TitleAr = descriptor.TitleAr;
        IconKey = descriptor.IconKey;
        Command = command;
    }
}
