using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Repositories;

public interface IUsersRepository
{
    Task<IReadOnlyList<UserListRow>> ListAsync(string? query, int? roleId, bool? activeOnly, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);
    Task<User?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Hashes <paramref name="m"/>.NewPassword via BCrypt. Throws when Username is already taken.</summary>
    Task<int> CreateAsync(UserSaveModel m, CancellationToken ct = default);

    /// <summary>Updates everything EXCEPT PasswordHash and Username (Username is immutable after creation).</summary>
    Task UpdateAsync(int id, UserSaveModel m, CancellationToken ct = default);

    Task ResetPasswordAsync(int id, string newPassword, CancellationToken ct = default);

    /// <summary>Verifies the current password via BCrypt before storing the new hash.</summary>
    Task ChangeOwnPasswordAsync(int id, string oldPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Refuses to deactivate the current signed-in user OR the last active admin.</summary>
    Task SetActiveAsync(int id, bool active, CancellationToken ct = default);

    /// <summary>
    /// Refuses to delete the current user OR the last admin OR any user with BackupLogs / Payments
    /// referencing them. Throws <see cref="InvalidOperationException"/> with an Arabic message.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Persists <paramref name="newPermissions"/> for the role. Refuses when removing
    /// <see cref="Permission.ManageUsers"/> from a role would leave the system with no admin.
    /// </summary>
    Task UpdateRolePermissionsAsync(int roleId, Permission newPermissions, CancellationToken ct = default);
}

/// <summary>Flat row returned by <see cref="IUsersRepository.ListAsync"/> for the Users grid.</summary>
public sealed record UserListRow(
    int Id,
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string RoleNameAr,
    int RoleId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

/// <summary>
/// Save payload for both Create and Update. <see cref="NewPassword"/> is only honored
/// on Create — password updates go through <see cref="IUsersRepository.ResetPasswordAsync"/>.
/// </summary>
public sealed record UserSaveModel(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    int RoleId,
    bool IsActive,
    string? NewPassword);
