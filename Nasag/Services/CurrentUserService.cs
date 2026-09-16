using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Nasag.Models;

namespace Nasag.Services;

public sealed partial class CurrentUserService : ObservableObject, ICurrentUserService
{
    [ObservableProperty]
    private User? _user;

    public bool IsAuthenticated => User is not null;

    public string DisplayName => User?.FullName ?? "مستخدم";

    public string Initial
    {
        get
        {
            var name = User?.FullName;
            if (string.IsNullOrWhiteSpace(name)) return "؟";
            var trimmed = name.TrimStart();
            return trimmed.Length == 0 ? "؟" : trimmed[..1];
        }
    }

    public event EventHandler? SignedIn;
    public event EventHandler? SignedOut;

    public void SignIn(User user)
    {
        User = user;
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Initial));
        SignedIn?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        if (User is null) return;
        User = null;
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Initial));
        SignedOut?.Invoke(this, EventArgs.Empty);
    }

    public bool HasPermission(Permission permission)
    {
        var role = User?.Role;
        if (role is null) return false;
        // None is a no-op; calling HasFlag(None) is always true which is misleading.
        if (permission == Permission.None) return false;
        return role.Permissions.HasFlag(permission);
    }
}
