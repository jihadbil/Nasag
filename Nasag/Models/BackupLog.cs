using System;

namespace Nasag.Models;

public class BackupLog
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long SizeBytes { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Distinguishes a backup creation log from a restore audit row. Defaults to
    /// <see cref="BackupKind.Backup"/> so existing rows (pre-Phase 12) read as backups.
    /// </summary>
    public BackupKind Kind { get; set; } = BackupKind.Backup;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
