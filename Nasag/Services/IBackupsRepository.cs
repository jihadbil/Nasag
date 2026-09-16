using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Services;

/// <summary>
/// Lightweight read/write surface for the <c>BackupLogs</c> table. Kept under
/// <c>Services/</c> (instead of <c>Repositories/</c>) so the whole Phase-12
/// Backup &amp; Restore stack — service + repo + DTOs — sits next to each other.
/// </summary>
public interface IBackupsRepository
{
    Task<IReadOnlyList<BackupLogRow>> ListAsync(int take = 100, CancellationToken ct = default);

    Task<BackupLog> AddLogAsync(
        string filePath,
        BackupKind kind,
        long sizeBytes,
        string? notes,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes the audit row only. The <c>.bak</c> file on disk is NOT removed
    /// (it may live on a network share, USB drive, etc — out of the app's reach).
    /// </summary>
    Task DeleteLogAsync(int id, CancellationToken ct = default);
}

/// <summary>Flat row used by the backup list grid (no nav-properties leak).</summary>
public sealed record BackupLogRow(
    int Id,
    string FilePath,
    BackupKind Kind,
    DateTime CreatedAt,
    long SizeBytes,
    string? Notes,
    string CreatedByName);
