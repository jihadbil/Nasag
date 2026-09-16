using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;
using Nasag.Services;

namespace Nasag.Repositories;

public sealed class UsersRepository : IUsersRepository
{
    // BCrypt cost factor. 11 ≈ 100ms on a typical desktop CPU — strong but not painful
    // during login. Matches the seeder's defaults so existing hashes remain compatible.
    private const int BcryptWorkFactor = 11;

    // Minimum acceptable password length when an admin resets a user's password or
    // a user changes their own password. Same threshold used by the VM, repeated
    // here as a defensive guard for direct callers (tests / future API).
    private const int MinPasswordLength = 6;

    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly ICurrentUserService _currentUser;

    public UsersRepository(IDbContextFactory<NasaqDbContext> factory, ICurrentUserService currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserListRow>> ListAsync(
        string? query, int? roleId, bool? activeOnly, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = ctx.Users.AsNoTracking().AsQueryable();

        if (roleId.HasValue)
            q = q.Where(u => u.RoleId == roleId.Value);

        // WHY: nullable bool — null means "show everyone", true means "only active",
        // false means "only deactivated". A checkbox in the toolbar passes true/null.
        if (activeOnly.HasValue)
            q = q.Where(u => u.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            q = q.Where(u =>
                EF.Functions.Like(u.Username, "%" + needle + "%") ||
                EF.Functions.Like(u.FullName, "%" + needle + "%") ||
                (u.Email != null && EF.Functions.Like(u.Email, "%" + needle + "%")) ||
                (u.Phone != null && EF.Functions.Like(u.Phone, "%" + needle + "%")));
        }

        // Project into the DTO directly so PasswordHash / PhotoPath never leave the DB.
        return await q
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Select(u => new UserListRow(
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role.NameAr,
                u.RoleId,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Roles
            .AsNoTracking()
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.NameAr)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(UserSaveModel m, CancellationToken ct = default)
    {
        if (m is null) throw new ArgumentNullException(nameof(m));
        EnsurePermission();

        ValidateUserModel(m, requirePassword: true);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var username = m.Username.Trim();

        // Case-insensitive uniqueness check. SQL Server is usually CI by default but
        // we don't want to depend on the collation — Lower() makes intent explicit.
        var lower = username.ToLowerInvariant();
        var taken = await ctx.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == lower, ct)
            .ConfigureAwait(false);
        if (taken)
            throw new InvalidOperationException("اسم المستخدم محجوز بالفعل. اختر اسماً آخر.");

        var role = await ctx.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == m.RoleId, ct)
            .ConfigureAwait(false);
        if (role is null)
            throw new InvalidOperationException("الدور المختار غير موجود.");

        var user = new User
        {
            Username = username,
            FullName = m.FullName.Trim(),
            Email = NormalizeOptional(m.Email),
            Phone = NormalizeOptional(m.Phone),
            RoleId = m.RoleId,
            IsActive = m.IsActive,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(m.NewPassword!, BcryptWorkFactor)
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return user.Id;
    }

    public async Task UpdateAsync(int id, UserSaveModel m, CancellationToken ct = default)
    {
        if (m is null) throw new ArgumentNullException(nameof(m));
        EnsurePermission();

        ValidateUserModel(m, requirePassword: false);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var user = await ctx.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود.");

        var role = await ctx.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == m.RoleId, ct)
            .ConfigureAwait(false);
        if (role is null)
            throw new InvalidOperationException("الدور المختار غير موجود.");

        // If the user being edited is currently the only admin and we're either
        // moving them out of an admin role or deactivating them — refuse.
        var movingOutOfAdmin = user.Role.Permissions.HasFlag(Permission.ManageUsers)
                               && !role.Permissions.HasFlag(Permission.ManageUsers);
        var deactivating = user.IsActive && !m.IsActive;
        if ((movingOutOfAdmin || deactivating) && await IsLastAdminAsync(ctx, user.Id, ct).ConfigureAwait(false))
            throw new InvalidOperationException("لا يمكن تنفيذ هذا التعديل — يجب أن يبقى مستخدم مدير واحد على الأقل.");

        // Refuse self-deactivation to avoid the user locking themselves out.
        if (deactivating && _currentUser.User?.Id == user.Id)
            throw new InvalidOperationException("لا يمكنك إيقاف حسابك الحالي.");

        // Username and PasswordHash intentionally untouched here.
        user.FullName = m.FullName.Trim();
        user.Email = NormalizeOptional(m.Email);
        user.Phone = NormalizeOptional(m.Phone);
        user.RoleId = m.RoleId;
        user.IsActive = m.IsActive;

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken ct = default)
    {
        EnsurePermission();
        ValidatePassword(newPassword);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var user = await ctx.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ChangeOwnPasswordAsync(int id, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(oldPassword))
            throw new InvalidOperationException("كلمة المرور الحالية مطلوبة.");
        ValidatePassword(newPassword);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var user = await ctx.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود.");

        bool verified;
        try
        {
            verified = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            verified = false;
        }
        if (!verified)
            throw new InvalidOperationException("كلمة المرور الحالية غير صحيحة.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        EnsurePermission();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var user = await ctx.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود.");

        if (!active)
        {
            if (_currentUser.User?.Id == user.Id)
                throw new InvalidOperationException("لا يمكنك إيقاف حسابك الحالي.");

            if (await IsLastAdminAsync(ctx, user.Id, ct).ConfigureAwait(false))
                throw new InvalidOperationException("لا يمكن إيقاف آخر مستخدم مدير في النظام.");
        }

        if (user.IsActive == active) return;
        user.IsActive = active;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        EnsurePermission();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var user = await ctx.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود.");

        if (_currentUser.User?.Id == user.Id)
            throw new InvalidOperationException("لا يمكنك حذف حسابك الحالي.");

        if (await IsLastAdminAsync(ctx, user.Id, ct).ConfigureAwait(false))
            throw new InvalidOperationException("لا يمكن حذف آخر مستخدم مدير في النظام.");

        // WHY: BackupLogs.CreatedByUserId and Payments.UserId both have OnDelete=Restrict
        // in the schema — deleting the row would surface as a SqlException with a
        // foreign-key error. We translate it into a clear Arabic message *before*
        // hitting SaveChangesAsync so the user sees the cause.
        var backupCount = await ctx.BackupLogs.CountAsync(b => b.CreatedByUserId == user.Id, ct).ConfigureAwait(false);
        if (backupCount > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف هذا المستخدم — يوجد {backupCount} سجل في النسخ الاحتياطي مرتبط به. أوقف حسابه بدلاً من حذفه.");

        var paymentCount = await ctx.Payments.CountAsync(p => p.UserId == user.Id, ct).ConfigureAwait(false);
        if (paymentCount > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف هذا المستخدم — يوجد {paymentCount} سند قبض مسجَّل باسمه. أوقف حسابه بدلاً من حذفه.");

        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateRolePermissionsAsync(int roleId, Permission newPermissions, CancellationToken ct = default)
    {
        EnsurePermission();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var role = await ctx.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, ct)
            .ConfigureAwait(false);
        if (role is null)
            throw new InvalidOperationException("الدور غير موجود.");

        var wasAdmin = role.Permissions.HasFlag(Permission.ManageUsers);
        var willBeAdmin = newPermissions.HasFlag(Permission.ManageUsers);

        // WHY: refuse to strip ManageUsers from the role currently held by the only
        // active admin. The instructions tie this to system roles; we apply the
        // guard to any role because the result is the same — the system would have
        // no admin left and recovery would be impossible.
        if (wasAdmin && !willBeAdmin)
        {
            var otherAdminCount = await ctx.Users
                .Where(u => u.IsActive
                            && u.RoleId != role.Id
                            && (u.Role.Permissions & Permission.ManageUsers) == Permission.ManageUsers)
                .CountAsync(ct)
                .ConfigureAwait(false);
            if (otherAdminCount == 0)
                throw new InvalidOperationException(
                    "لا يمكن إزالة صلاحية إدارة المستخدمين — يجب أن يبقى دور مدير واحد على الأقل.");
        }

        role.Permissions = newPermissions;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ----- helpers ----------------------------------------------------------

    /// <summary>
    /// True when removing/disabling <paramref name="userId"/> would leave zero
    /// active users whose role has <see cref="Permission.ManageUsers"/>.
    /// </summary>
    private static async Task<bool> IsLastAdminAsync(NasaqDbContext ctx, int userId, CancellationToken ct)
    {
        var remainingAdmins = await ctx.Users
            .Where(u => u.Id != userId
                        && u.IsActive
                        && (u.Role.Permissions & Permission.ManageUsers) == Permission.ManageUsers)
            .CountAsync(ct)
            .ConfigureAwait(false);
        return remainingAdmins == 0;
    }

    private void EnsurePermission()
    {
        if (!_currentUser.HasPermission(Permission.ManageUsers))
            throw new UnauthorizedAccessException("ليس لديك صلاحية إدارة المستخدمين.");
    }

    private static void ValidateUserModel(UserSaveModel m, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(m.Username))
            throw new InvalidOperationException("اسم المستخدم مطلوب.");
        if (m.Username.Trim().Length < 3)
            throw new InvalidOperationException("اسم المستخدم يجب أن يكون 3 أحرف فأكثر.");
        if (string.IsNullOrWhiteSpace(m.FullName))
            throw new InvalidOperationException("الاسم الكامل مطلوب.");
        if (m.RoleId <= 0)
            throw new InvalidOperationException("الرجاء اختيار دور للمستخدم.");
        if (requirePassword)
            ValidatePassword(m.NewPassword);
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("كلمة المرور مطلوبة.");
        if (password.Length < MinPasswordLength)
            throw new InvalidOperationException($"كلمة المرور يجب أن تكون {MinPasswordLength} أحرف فأكثر.");
    }

    private static string? NormalizeOptional(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
