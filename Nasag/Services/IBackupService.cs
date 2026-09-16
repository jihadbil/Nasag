using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

/// <summary>
/// Backup / restore operations for the live SQL Server database. Implementations
/// must run BACKUP / RESTORE via raw <c>Microsoft.Data.SqlClient.SqlConnection</c>
/// — these cannot pass through EF Core because <c>Database.MigrateAsync</c> wraps
/// commands in a transaction that SQL Server forbids for backup/restore.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a <c>.bak</c> file inside <paramref name="targetFolder"/> using
    /// <c>BACKUP DATABASE [NasaqSchoolDb] TO DISK = @path WITH FORMAT, INIT, COMPRESSION</c>.
    /// If <c>COMPRESSION</c> is rejected (LocalDB does not support backup compression),
    /// retries once without it. Persists a <see cref="Nasag.Models.BackupLog"/> row
    /// keyed to the current user.
    /// </summary>
    Task<BackupResult> CreateBackupAsync(
        string targetFolder,
        string? notes,
        IProgress<string>? progress,
        CancellationToken ct = default);

    /// <summary>
    /// Restores the database from <paramref name="backupPath"/> by connecting to the
    /// <c>master</c> database, switching the target to SINGLE_USER, running RESTORE,
    /// and switching back to MULTI_USER. Logs the audit row BEFORE flipping to single
    /// user — once the restore completes, the in-process user identity is wiped, so
    /// the audit must be persisted while the original DbContext can still write.
    /// </summary>
    Task<RestoreResult> RestoreBackupAsync(
        string backupPath,
        IProgress<string>? progress,
        CancellationToken ct = default);
}

public sealed record BackupResult(string FilePath, long SizeBytes);

public sealed record RestoreResult(bool Success, string? ErrorMessage);
