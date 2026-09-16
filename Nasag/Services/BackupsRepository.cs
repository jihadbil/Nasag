using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Services;

public sealed class BackupsRepository : IBackupsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly ICurrentUserService _currentUser;

    public BackupsRepository(IDbContextFactory<NasaqDbContext> factory, ICurrentUserService currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<BackupLogRow>> ListAsync(int take = 100, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageBackup);

        if (take <= 0) take = 100;
        if (take > 500) take = 500;

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.BackupLogs
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Take(take)
            .Select(b => new BackupLogRow(
                b.Id,
                b.FilePath,
                b.Kind,
                b.CreatedAt,
                b.SizeBytes,
                b.Notes,
                b.CreatedByUser != null ? b.CreatedByUser.FullName : "—"))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<BackupLog> AddLogAsync(
        string filePath,
        BackupKind kind,
        long sizeBytes,
        string? notes,
        CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageBackup);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidOperationException("مسار الملف مطلوب.");

        var userId = _currentUser.User?.Id
                     ?? throw new InvalidOperationException("لا يوجد مستخدم مسجّل دخوله.");

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new BackupLog
        {
            FilePath = filePath.Length <= 400 ? filePath : filePath[..400],
            Kind = kind,
            CreatedAt = DateTime.UtcNow,
            SizeBytes = sizeBytes,
            Notes = NormalizeNotes(notes),
            CreatedByUserId = userId
        };

        ctx.BackupLogs.Add(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task DeleteLogAsync(int id, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageBackup);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await ctx.BackupLogs
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }

    private static string? NormalizeNotes(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length <= 300 ? text : text[..300];
    }

    private void EnsurePermission(Permission required)
    {
        if (!_currentUser.HasPermission(required))
            throw new UnauthorizedAccessException("ليس لديك صلاحية إدارة النسخ الاحتياطي.");
    }
}
