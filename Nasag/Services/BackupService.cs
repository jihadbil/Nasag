using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Services;

/// <summary>
/// Concrete implementation backed by ADO.NET (Microsoft.Data.SqlClient) for the
/// BACKUP / RESTORE statements and EF Core for the audit log writes.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly IConfiguration _config;
    private readonly ICurrentUserService _currentUser;
    private readonly IConnectionMonitor _connection;

    public BackupService(
        IDbContextFactory<NasaqDbContext> factory,
        IConfiguration config,
        ICurrentUserService currentUser,
        IConnectionMonitor connection)
    {
        _factory = factory;
        _config = config;
        _currentUser = currentUser;
        _connection = connection;
    }

    public async Task<BackupResult> CreateBackupAsync(
        string targetFolder,
        string? notes,
        IProgress<string>? progress,
        CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageBackup);

        if (string.IsNullOrWhiteSpace(targetFolder))
            throw new InvalidOperationException("الرجاء اختيار مجلد لحفظ النسخة الاحتياطية.");

        Directory.CreateDirectory(targetFolder);

        var (appConnectionString, databaseName) = ResolveConnection();
        if (!IsSafeDatabaseName(databaseName))
            throw new InvalidOperationException("اسم قاعدة البيانات يحتوي على رموز غير مسموحة.");
        var fileName = $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(targetFolder, fileName);

        progress?.Report("جاري إنشاء النسخة الاحتياطية…");

        // WHY: bracket-quoted DB names are safe because the value comes from
        // appsettings.json (not user input). The PATH is user-provided so it MUST
        // travel through a SqlParameter.
        // WHY: try with COMPRESSION first (faster, smaller), fall back without it
        // because LocalDB rejects it with "BACKUP DATABASE WITH COMPRESSION is not
        // supported on this edition of SQL Server" (msg 1844).
        try
        {
            await RunBackupAsync(appConnectionString, databaseName, fullPath, withCompression: true, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Number == 1844 || ex.Message.Contains("COMPRESSION", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report("هذه النسخة من SQL Server لا تدعم الضغط — إعادة المحاولة بدون ضغط…");
            await RunBackupAsync(appConnectionString, databaseName, fullPath, withCompression: false, ct)
                .ConfigureAwait(false);
        }

        _connection.ReportSuccess();

        long size = 0;
        try { size = new FileInfo(fullPath).Length; }
        catch { /* file existence verified by SQL Server; size best-effort */ }

        progress?.Report("جاري حفظ سجل العملية…");

        // Persist the audit row (Kind=Backup) — this is a normal EF Core call;
        // the DB is still online and the user identity is intact.
        await using (var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false))
        {
            ctx.BackupLogs.Add(new BackupLog
            {
                FilePath = fullPath,
                CreatedAt = DateTime.UtcNow,
                SizeBytes = size,
                Notes = NormalizeNotes(notes),
                Kind = BackupKind.Backup,
                CreatedByUserId = _currentUser.User?.Id
                                  ?? throw new InvalidOperationException("لا يوجد مستخدم مسجّل دخوله.")
            });
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new BackupResult(fullPath, size);
    }

    public async Task<RestoreResult> RestoreBackupAsync(
        string backupPath,
        IProgress<string>? progress,
        CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageBackup);

        if (string.IsNullOrWhiteSpace(backupPath))
            return new RestoreResult(false, "الرجاء اختيار ملف نسخة احتياطية صالح.");
        if (!File.Exists(backupPath))
            return new RestoreResult(false, "ملف النسخة الاحتياطية غير موجود.");

        var (appConnectionString, databaseName) = ResolveConnection();
        if (!IsSafeDatabaseName(databaseName))
            throw new InvalidOperationException("اسم قاعدة البيانات يحتوي على رموز غير مسموحة.");
        var masterConnectionString = BuildMasterConnectionString(appConnectionString);
        _ = _currentUser.User?.Id
            ?? throw new InvalidOperationException("لا يوجد مستخدم مسجّل دخوله.");

        // WHY no DB-side Restore audit row: RESTORE … WITH REPLACE overwrites the
        // entire database with the .bak snapshot, so any row inserted here would
        // be discarded the moment the restore completes. The act of restoring is
        // intentionally invisible in BackupLogs post-restore (the Backup row that
        // produced the .bak remains the source of truth). The app shuts down
        // immediately after a successful restore so the user has a clean restart.

        // Free pooled contexts so the SINGLE_USER flip below can succeed.
        await Task.Yield();
        bool restored = false;
        try
        {
            progress?.Report("جاري قفل قاعدة البيانات…");
            await using var conn = new SqlConnection(masterConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            try
            {
                // Force-kick all other connections to NasaqSchoolDb.
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = $"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NOT NULL " +
                                       $"BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; END";
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                progress?.Report("جاري استعادة قاعدة البيانات من الملف…");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = $"RESTORE DATABASE [{databaseName}] FROM DISK = @path WITH REPLACE";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@path";
                    p.DbType = DbType.String;
                    p.Size = 400;
                    p.Value = backupPath;
                    cmd.Parameters.Add(p);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                restored = true;
            }
            finally
            {
                // Always try to bring the DB back to MULTI_USER, even on failure —
                // otherwise the app remains locked out forever.
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = $"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NOT NULL " +
                                       $"BEGIN ALTER DATABASE [{databaseName}] SET MULTI_USER; END";
                    await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Best-effort recovery; the user will see the original failure.
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            return new RestoreResult(false, $"تعذّر استعادة قاعدة البيانات: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new RestoreResult(false, $"تعذّر استعادة قاعدة البيانات: {ex.Message}");
        }

        return restored
            ? new RestoreResult(true, null)
            : new RestoreResult(false, "فشل غير معروف أثناء الاستعادة.");
    }

    private static async Task RunBackupAsync(
        string connectionString,
        string databaseName,
        string fullPath,
        bool withCompression,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 0; // BACKUP can take a while on large DBs.
        var options = withCompression
            ? "WITH FORMAT, INIT, COMPRESSION"
            : "WITH FORMAT, INIT";
        cmd.CommandText = $"BACKUP DATABASE [{databaseName}] TO DISK = @path {options}";
        var p = cmd.CreateParameter();
        p.ParameterName = "@path";
        p.DbType = DbType.String;
        p.Size = 400;
        p.Value = fullPath;
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private (string ConnectionString, string DatabaseName) ResolveConnection()
    {
        var cs = _config.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection غير معرّفة.");
        var builder = new SqlConnectionStringBuilder(cs);
        var db = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(db))
            throw new InvalidOperationException("اسم قاعدة البيانات غير محدد في إعدادات الاتصال.");
        return (cs, db);
    }

    private static string BuildMasterConnectionString(string original)
    {
        // Clone the builder so we can keep server / auth / TrustServerCertificate
        // settings while pointing at master. We also disable MARS and pooling for
        // the restore connection — a single-shot ADO.NET call needs neither and
        // pooling can resurrect a dead session pointing back at the restored DB.
        var builder = new SqlConnectionStringBuilder(original)
        {
            InitialCatalog = "master",
            Pooling = false,
            MultipleActiveResultSets = false
        };
        return builder.ConnectionString;
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

    private static bool IsSafeDatabaseName(string name)
    {
        // Defensive: the value comes from appsettings.json, not user input, but we
        // interpolate it into a bracket-quoted identifier so reject anything that
        // contains characters that would break bracket-quoting or open injection.
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 128) return false;
        foreach (var c in name)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' '))
                return false;
        }
        return true;
    }
}
