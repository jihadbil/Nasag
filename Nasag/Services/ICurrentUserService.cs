using System;
using Nasag.Models;

namespace Nasag.Services;

public interface ICurrentUserService
{
    User? User { get; }
    bool IsAuthenticated { get; }

    /// <summary>The display name shown across the shell. Falls back to "مستخدم" when not signed in.</summary>
    string DisplayName { get; }

    /// <summary>Single uppercase letter used in the avatar circle.</summary>
    string Initial { get; }

    event EventHandler? SignedIn;
    event EventHandler? SignedOut;

    void SignIn(User user);
    void SignOut();

    /// <summary>
    /// Returns true when the signed-in user's role contains the requested permission flag.
    /// Returns false when no user is signed in or the role has no permissions.
    /// </summary>
    bool HasPermission(Permission permission);
}
