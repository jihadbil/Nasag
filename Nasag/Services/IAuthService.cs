using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Services;

public enum AuthFailureReason
{
    None = 0,
    InvalidCredentials = 1,
    AccountDisabled = 2,
    ConnectionError = 3,
    Unknown = 99
}

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }
    public User? User { get; init; }
    public AuthFailureReason Reason { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Ok(User user) => new() { IsSuccess = true, User = user };

    public static AuthResult Fail(AuthFailureReason reason, string message) =>
        new() { IsSuccess = false, Reason = reason, ErrorMessage = message };
}

public interface IAuthService
{
    Task<AuthResult> SignInAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Changes the signed-in user's password. Throws <see cref="System.InvalidOperationException"/>
    /// when no user is signed in, the old password is wrong, or the new one fails policy.
    /// </summary>
    Task ChangeOwnPasswordAsync(string oldPassword, string newPassword, CancellationToken ct = default);
}
