using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Models;
using Nasag.Repositories;

namespace Nasag.ViewModels.Pages.Users;

public enum UserEditorMode { Add, Edit }

/// <summary>
/// ViewModel for the Add/Edit user dialog. Password fields are only relevant
/// when <see cref="IsAddMode"/> is true — edits go through the dedicated reset dialog.
/// </summary>
public sealed partial class UserEditorDialogViewModel : ObservableObject
{
    private readonly IUsersRepository _users;
    private int _editingId;

    /// <summary>
    /// Raised after a successful save. The host dialog should close the window
    /// and the parent VM should refresh its grid.
    /// </summary>
    public event EventHandler? Saved;

    /// <summary>Raised when the user dismisses the dialog without saving.</summary>
    public event EventHandler? Cancelled;

    public UserEditorDialogViewModel(IUsersRepository users)
    {
        _users = users;
    }

    public ObservableCollection<Role> Roles { get; } = new();

    [ObservableProperty] private UserEditorMode _mode = UserEditorMode.Add;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private Role? _selectedRole;
    [ObservableProperty] private bool _isActive = true;

    // Password fields. PasswordBox can't TwoWay-bind so the View code-behind
    // pushes the current values via setters on PasswordChanged.
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanSave => !IsBusy;

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSave));

    public bool IsAddMode => Mode == UserEditorMode.Add;
    public bool IsEditMode => Mode == UserEditorMode.Edit;
    public string TitleAr => IsAddMode ? "إضافة مستخدم جديد" : "تعديل بيانات المستخدم";

    partial void OnModeChanged(UserEditorMode value)
    {
        OnPropertyChanged(nameof(IsAddMode));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(TitleAr));
    }

    public async Task LoadForCreateAsync(CancellationToken ct = default)
    {
        Mode = UserEditorMode.Add;
        _editingId = 0;
        Username = string.Empty;
        FullName = string.Empty;
        Email = null;
        Phone = null;
        IsActive = true;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;

        await LoadRolesAsync(ct).ConfigureAwait(true);
        // Pick the first non-system role by default so a new account isn't accidentally created as admin.
        SelectedRole ??= GetDefaultRole();
    }

    public async Task LoadForEditAsync(int userId, CancellationToken ct = default)
    {
        Mode = UserEditorMode.Edit;
        _editingId = userId;
        ErrorMessage = null;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;

        await LoadRolesAsync(ct).ConfigureAwait(true);

        var user = await _users.GetAsync(userId, ct).ConfigureAwait(true);
        if (user is null)
        {
            ErrorMessage = "المستخدم غير موجود.";
            return;
        }

        Username = user.Username;
        FullName = user.FullName;
        Email = user.Email;
        Phone = user.Phone;
        IsActive = user.IsActive;
        SelectedRole = FindRole(user.RoleId) ?? GetDefaultRole();
    }

    private async Task LoadRolesAsync(CancellationToken ct)
    {
        var roles = await _users.GetRolesAsync(ct).ConfigureAwait(true);
        Roles.Clear();
        foreach (var r in roles) Roles.Add(r);
    }

    private Role? FindRole(int roleId)
    {
        foreach (var r in Roles)
            if (r.Id == roleId) return r;
        return null;
    }

    private Role? GetDefaultRole()
    {
        foreach (var r in Roles)
            if (!r.IsSystem) return r;
        return Roles.Count > 0 ? Roles[0] : null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        // Inline validation — repo also validates as a defensive measure but we want
        // the dialog to feel responsive without a round-trip for obvious errors.
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "اسم المستخدم مطلوب.";
            return;
        }
        if (Username.Trim().Length < 3)
        {
            ErrorMessage = "اسم المستخدم يجب أن يكون 3 أحرف فأكثر.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "الاسم الكامل مطلوب.";
            return;
        }
        if (SelectedRole is null)
        {
            ErrorMessage = "الرجاء اختيار دور.";
            return;
        }
        if (IsAddMode)
        {
            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف فأكثر.";
                return;
            }
            if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
            {
                ErrorMessage = "كلمتا المرور غير متطابقتين.";
                return;
            }
        }

        try
        {
            IsBusy = true;

            var model = new UserSaveModel(
                Username: Username.Trim(),
                FullName: FullName.Trim(),
                Email: string.IsNullOrWhiteSpace(Email) ? null : Email!.Trim(),
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone!.Trim(),
                RoleId: SelectedRole!.Id,
                IsActive: IsActive,
                NewPassword: IsAddMode ? NewPassword : null);

            if (IsAddMode)
                await _users.CreateAsync(model).ConfigureAwait(true);
            else
                await _users.UpdateAsync(_editingId, model).ConfigureAwait(true);

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Intentionally rethrow — the dialog isn't tied to a long-running token but
            // we still must not mask cancellations as user-visible errors.
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
