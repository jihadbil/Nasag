using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Repositories;

namespace Nasag.ViewModels.Pages.Users;

/// <summary>
/// ViewModel for the "reset another user's password" dialog. The two PasswordBox
/// values are pushed in from code-behind (PasswordBox cannot TwoWay-bind for
/// security reasons).
/// </summary>
public sealed partial class PasswordResetDialogViewModel : ObservableObject
{
    private readonly IUsersRepository _users;
    private int _targetUserId;

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public PasswordResetDialogViewModel(IUsersRepository users)
    {
        _users = users;
    }

    [ObservableProperty] private string _targetUserName = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanSave => !IsBusy;

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSave));

    public void LoadFor(int userId, string fullName)
    {
        _targetUserId = userId;
        TargetUserName = fullName;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

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

        try
        {
            IsBusy = true;
            await _users.ResetPasswordAsync(_targetUserId, NewPassword).ConfigureAwait(true);
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
