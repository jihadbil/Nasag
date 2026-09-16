using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.ViewModels.Pages;

namespace Nasag.ViewModels.Pages.Users;

/// <summary>
/// Page ViewModel for "Users &amp; Roles". Lists users with role/active filters,
/// drives the Add/Edit/Reset/Delete dialogs, and gates everything on the
/// <see cref="Permission.ManageUsers"/> permission.
/// </summary>
public sealed partial class UsersViewModel : PageViewModel
{
    private readonly IUsersRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _services;

    private CancellationTokenSource? _searchCts;
    private bool _reloadInFlight;

    /// <summary>
    /// True while the constructor is wiring up backing fields. Used by partial
    /// OnXxxChanged callbacks to skip cascading reloads (see AI_INSTRUCTIONS §13).
    /// </summary>
    private bool _isInitializing = true;

    public UsersViewModel(
        IUsersRepository repo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        ICurrentUserService currentUser,
        IServiceProvider services)
    {
        _repo = repo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _currentUser = currentUser;
        _services = services;

        _currentUser.SignedIn += (_, _) => RefreshPermissions();
        _currentUser.SignedOut += (_, _) => RefreshPermissions();

        // Assign backing fields directly so source-generated setters don't fire
        // OnXxxChanged callbacks (and trigger reloads) during construction.
        _activeOnly = true;
        _canManageUsers = _currentUser.HasPermission(Permission.ManageUsers);

        _isInitializing = false;
    }

    public override string TitleAr => "المستخدمون";
    public override string SubtitleAr => "إدارة المستخدمين والأدوار والصلاحيات";

    public ObservableCollection<UserListRow> Rows { get; } = new();
    public ObservableCollection<RoleOption> RoleOptions { get; } = new();

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private RoleOption? _selectedRole;
    [ObservableProperty] private bool _activeOnly;
    [ObservableProperty] private UserListRow? _selectedRow;
    [ObservableProperty] private bool _canManageUsers;

    public bool HasResults => Rows.Count > 0;
    public bool IsEmpty => !IsLoading && Rows.Count == 0;
    public int TotalCount => Rows.Count;
    public string StatsLine => $"إجمالي المستخدمين: {Rows.Count:N0}";

    private void RefreshPermissions()
    {
        CanManageUsers = _currentUser.HasPermission(Permission.ManageUsers);
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (_isInitializing) return;
        DebouncedReload();
    }

    partial void OnSelectedRoleChanged(RoleOption? value)
    {
        if (_isInitializing) return;
        _ = ReloadAsync();
    }

    partial void OnActiveOnlyChanged(bool value)
    {
        if (_isInitializing) return;
        _ = ReloadAsync();
    }

    partial void OnSelectedRowChanged(UserListRow? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ResetPasswordCommand.NotifyCanExecuteChanged();
        ToggleActiveCommand.NotifyCanExecuteChanged();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (RoleOptions.Count == 0)
            await LoadRolesAsync(ct).ConfigureAwait(true);
        await ReloadAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadRolesAsync(CancellationToken ct)
    {
        try
        {
            var roles = await _repo.GetRolesAsync(ct).ConfigureAwait(true);
            RoleOptions.Clear();
            foreach (var r in roles)
                RoleOptions.Add(new RoleOption(r.Id, r.NameAr));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحميل قائمة الأدوار", ex.Message, ex);
        }
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight) return;
        _reloadInFlight = true;
        try
        {
            await ReloadCoreAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    private async Task ReloadCoreAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            StatusMessage = null;

            var query = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();
            // ActiveOnly == true → only active rows; false → show everyone. The
            // repo treats null as "no filter" which matches the latter.
            bool? activeFilter = ActiveOnly ? true : (bool?)null;

            var rows = await _repo.ListAsync(query, SelectedRole?.Id, activeFilter, ct).ConfigureAwait(true);

            Rows.Clear();
            foreach (var r in rows) Rows.Add(r);

            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(StatsLine));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل قائمة المستخدمين.";
            _errors.Report("تعذّر تحميل قائمة المستخدمين", ex.Message, ex);
        }
    }

    private void DebouncedReload()
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, cts.Token).ConfigureAwait(false);
                if (cts.IsCancellationRequested) return;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ReloadAsync(cts.Token));
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedRole = null;
        ActiveOnly = true;
        SearchQuery = string.Empty;
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        try
        {
            var vm = _services.GetRequiredService<UserEditorDialogViewModel>();
            await vm.LoadForCreateAsync().ConfigureAwait(true);
            var saved = Nasag.Views.Pages.Users.UserEditorDialog.Show(vm);
            if (saved)
            {
                _toasts.Success("تم إضافة المستخدم", vm.FullName);
                await ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إضافة المستخدم", ex.Message, ex);
        }
    }

    private bool CanActOnRow(UserListRow? row) => row is not null && CanManageUsers;

    [RelayCommand(CanExecute = nameof(CanActOnRow))]
    private async Task EditAsync(UserListRow? row)
    {
        if (row is null) return;
        try
        {
            var vm = _services.GetRequiredService<UserEditorDialogViewModel>();
            await vm.LoadForEditAsync(row.Id).ConfigureAwait(true);
            var saved = Nasag.Views.Pages.Users.UserEditorDialog.Show(vm);
            if (saved)
            {
                _toasts.Success("تم حفظ التغييرات", row.FullName);
                await ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تعديل المستخدم", ex.Message, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnRow))]
    private async Task DeleteAsync(UserListRow? row)
    {
        if (row is null) return;

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف مستخدم",
            $"سيتم حذف المستخدم «{row.FullName}» نهائياً. لا يمكن التراجع عن هذا الإجراء.",
            okText: "حذف نهائي").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.DeleteAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف المستخدم", row.FullName);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException ex)
        {
            // Business-rule rejection (last admin / has BackupLogs / has Payments) —
            // surface as a friendly warning rather than the technical error window.
            await _dialogs.ShowWarningAsync("تعذّر الحذف", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف المستخدم", ex.Message, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnRow))]
    private async Task ToggleActiveAsync(UserListRow? row)
    {
        if (row is null) return;
        var target = !row.IsActive;
        var verbAr = target ? "تفعيل" : "إيقاف";

        var ok = await _dialogs.ConfirmAsync(
            $"{verbAr} المستخدم",
            $"هل تريد {verbAr} الحساب «{row.FullName}»؟").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.SetActiveAsync(row.Id, target).ConfigureAwait(true);
            _toasts.Success($"تم {verbAr} الحساب", row.FullName);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("غير مسموح", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report($"تعذّر {verbAr} المستخدم", ex.Message, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnRow))]
    private async Task ResetPasswordAsync(UserListRow? row)
    {
        if (row is null) return;
        try
        {
            var vm = _services.GetRequiredService<PasswordResetDialogViewModel>();
            vm.LoadFor(row.Id, row.FullName);
            var saved = Nasag.Views.Pages.Users.PasswordResetDialog.Show(vm);
            if (saved)
                _toasts.Success("تم تعيين كلمة المرور", row.FullName);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إعادة تعيين كلمة المرور", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task ManageRolesAsync()
    {
        try
        {
            var vm = _services.GetRequiredService<RoleEditorDialogViewModel>();
            await vm.LoadAsync().ConfigureAwait(true);
            var saved = Nasag.Views.Pages.Users.RoleEditorDialog.Show(vm);
            if (saved)
            {
                _toasts.Success("تم حفظ الصلاحيات", null);
                // Refresh permissions for the live shell — the current user's role
                // might have just changed, which affects sidebar visibility.
                RefreshPermissions();
                await ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح محرر الأدوار", ex.Message, ex);
        }
    }
}

/// <summary>Compact dropdown option for the role filter.</summary>
public sealed record RoleOption(int Id, string NameAr);
