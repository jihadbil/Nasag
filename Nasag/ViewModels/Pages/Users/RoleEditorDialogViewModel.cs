using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Models;
using Nasag.Repositories;

namespace Nasag.ViewModels.Pages.Users;

/// <summary>
/// Arabic display names for the <see cref="Permission"/> flags. Used by the role
/// editor checkboxes — keep this in sync with the enum in Models/Enums.cs.
/// </summary>
public static class PermissionLabels
{
    public static readonly IReadOnlyList<(Permission Flag, string Label)> All = new[]
    {
        (Permission.ViewDashboard,    "عرض لوحة التحكم"),
        (Permission.ManageStudents,   "إدارة الطلاب"),
        (Permission.ManageClasses,    "إدارة الصفوف"),
        (Permission.ManageAttendance, "إدارة الحضور"),
        (Permission.ManageSubjects,   "إدارة المواد"),
        (Permission.ManageMarks,      "إدارة الدرجات"),
        (Permission.ViewResults,      "عرض النتائج"),
        (Permission.ManageFees,       "إدارة الرسوم"),
        (Permission.ManageReports,    "إدارة التقارير"),
        (Permission.ManageUsers,      "إدارة المستخدمين"),
        (Permission.ManageSettings,   "الإعدادات"),
        (Permission.ManageBackup,     "النسخ الاحتياطي"),
    };

    public static string For(Permission flag)
    {
        foreach (var (f, l) in All)
            if (f == flag) return l;
        return flag.ToString();
    }
}

/// <summary>
/// One checkbox entry inside <see cref="RoleRowViewModel"/>. <see cref="IsLocked"/>
/// is true when unticking would leave the system without an admin — the View renders
/// it disabled to preserve recovery access.
/// </summary>
public sealed partial class PermissionCheckViewModel : ObservableObject
{
    public PermissionCheckViewModel(Permission flag, string label, bool isChecked, bool isLocked)
    {
        Flag = flag;
        Label = label;
        _isChecked = isChecked;
        _isLocked = isLocked;
    }

    public Permission Flag { get; }
    public string Label { get; }

    [ObservableProperty] private bool _isChecked;
    [ObservableProperty] private bool _isLocked;
}

/// <summary>
/// One role row inside <see cref="RoleEditorDialogViewModel.RoleRows"/>. The
/// checkboxes are flat <see cref="PermissionCheckViewModel"/> entries so the View
/// can render them in a uniform grid.
/// </summary>
public sealed partial class RoleRowViewModel : ObservableObject
{
    public RoleRowViewModel(Role role, bool lockManageUsers)
    {
        RoleId = role.Id;
        NameAr = role.NameAr;
        IsSystem = role.IsSystem;
        Checks = new ObservableCollection<PermissionCheckViewModel>();
        foreach (var (flag, label) in PermissionLabels.All)
        {
            var isChecked = role.Permissions.HasFlag(flag);
            // Lock the ManageUsers checkbox on the admin row that's currently the only one.
            var locked = lockManageUsers && flag == Permission.ManageUsers;
            Checks.Add(new PermissionCheckViewModel(flag, label, isChecked, locked));
        }
    }

    public int RoleId { get; }
    public string NameAr { get; }
    public bool IsSystem { get; }
    public ObservableCollection<PermissionCheckViewModel> Checks { get; }

    /// <summary>Reduces the checkbox state back to a single bitmask for persistence.</summary>
    public Permission BuildPermissions()
    {
        var p = Permission.None;
        foreach (var c in Checks)
            if (c.IsChecked) p |= c.Flag;
        return p;
    }
}

/// <summary>
/// ViewModel for the "manage roles &amp; permissions" dialog. Loads every role,
/// lets the user tick/untick each permission, and persists per-role on Save.
/// </summary>
public sealed partial class RoleEditorDialogViewModel : ObservableObject
{
    private readonly IUsersRepository _users;

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public RoleEditorDialogViewModel(IUsersRepository users)
    {
        _users = users;
    }

    public ObservableCollection<RoleRowViewModel> RoleRows { get; } = new();

    [ObservableProperty] private RoleRowViewModel? _selectedRow;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoading;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanSave => !IsBusy && !IsLoading;

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSave));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanSave));

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        RoleRows.Clear();
        try
        {
            var roles = await _users.GetRolesAsync().ConfigureAwait(true);

            // Find the role currently held by the *only* active admin (if any).
            // The ManageUsers checkbox on that role will be locked so the user
            // can't accidentally strip themselves of recovery access.
            int? lockedRoleId = null;
            var adminRoles = roles.Where(r => r.Permissions.HasFlag(Permission.ManageUsers)).ToList();
            if (adminRoles.Count == 1)
                lockedRoleId = adminRoles[0].Id;

            foreach (var role in roles)
                RoleRows.Add(new RoleRowViewModel(role, lockManageUsers: role.Id == lockedRoleId));

            SelectedRow = RoleRows.Count > 0 ? RoleRows[0] : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        try
        {
            IsBusy = true;
            // Persist each role independently — the repo enforces the "last admin"
            // guard per call so we don't have to coordinate across them here.
            foreach (var row in RoleRows)
            {
                var perms = row.BuildPermissions();
                await _users.UpdateRolePermissionsAsync(row.RoleId, perms).ConfigureAwait(true);
            }
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
