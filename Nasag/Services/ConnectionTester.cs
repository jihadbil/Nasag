using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Nasag.Services;

/// <summary>
/// ADO.NET-backed implementation that probes / creates SQL Server databases
/// without touching EF Core (mirrors the BackupService pattern).
/// </summary>
public sealed class ConnectionTester : IConnectionTester
{
    public async Task<ConnectionTestResult> TestAsync(string connectionString, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new ConnectionTestResult(false, "سلسلة الاتصال فارغة.", null, false);

        SqlConnectionStringBuilder originalBuilder;
        try
        {
            originalBuilder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(
                false,
                "سلسلة الاتصال غير صالحة.",
                ex.Message,
                false);
        }

        var targetDb = originalBuilder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDb))
        {
            return new ConnectionTestResult(
                false,
                "لم يتم تحديد اسم قاعدة البيانات.",
                null,
                false);
        }

        // Build a DB-agnostic variant that points at master so we can verify
        // the server is reachable even when the target DB doesn't exist yet.
        var masterBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            Pooling = false,
            ConnectTimeout = 8
        };

        try
        {
            await using var conn = new SqlConnection(masterBuilder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            bool exists;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sys.databases WHERE name = @name";
                var p = cmd.CreateParameter();
                p.ParameterName = "@name";
                p.DbType = DbType.String;
                p.Size = 128;
                p.Value = targetDb;
                cmd.Parameters.Add(p);

                var row = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                exists = row is not null && row is not DBNull;
            }

            var message = exists
                ? "تم الاتصال بنجاح."
                : "تم الاتصال بنجاح. القاعدة غير موجودة بعد.";

            return new ConnectionTestResult(true, message, null, exists);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            return new ConnectionTestResult(
                false,
                "تعذّر الاتصال بخادم قاعدة البيانات. تحقق من اسم الخادم وبيانات الاعتماد.",
                FormatDetails(ex),
                false);
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(
                false,
                "حدث خطأ غير متوقع أثناء محاولة الاتصال.",
                FormatDetails(ex),
                false);
        }
    }

    public async Task<ConnectionTestResult> CreateDatabaseAsync(string connectionString, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new ConnectionTestResult(false, "سلسلة الاتصال فارغة.", null, false);

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(
                false,
                "سلسلة الاتصال غير صالحة.",
                ex.Message,
                false);
        }

        var targetDb = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDb))
            return new ConnectionTestResult(false, "لم يتم تحديد اسم قاعدة البيانات.", null, false);

        if (!IsSafeDatabaseName(targetDb))
        {
            return new ConnectionTestResult(
                false,
                "اسم قاعدة البيانات يحوي رموزاً غير مسموحة.",
                null,
                false);
        }

        var masterBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            Pooling = false,
            ConnectTimeout = 15
        };

        try
        {
            await using var conn = new SqlConnection(masterBuilder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            // Verify it doesn't already exist (cheap idempotency).
            await using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT 1 FROM sys.databases WHERE name = @name";
                var p = checkCmd.CreateParameter();
                p.ParameterName = "@name";
                p.DbType = DbType.String;
                p.Size = 128;
                p.Value = targetDb;
                checkCmd.Parameters.Add(p);

                var row = await checkCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                if (row is not null && row is not DBNull)
                {
                    return new ConnectionTestResult(
                        true,
                        $"قاعدة البيانات «{targetDb}» موجودة مسبقاً.",
                        null,
                        true);
                }
            }

            // Use a separate command for the CREATE — keeps the call site simple
            // and avoids confusion with the SELECT above. CommandTimeout=0 because
            // file-allocation on a fresh DB can take longer than the default 30s.
            await using (var createCmd = conn.CreateCommand())
            {
                createCmd.CommandTimeout = 0;
                createCmd.CommandText = $"CREATE DATABASE [{targetDb}]";
                await createCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            return new ConnectionTestResult(
                true,
                $"تم إنشاء قاعدة البيانات «{targetDb}» بنجاح.",
                null,
                true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            return new ConnectionTestResult(
                false,
                "تعذّر إنشاء قاعدة البيانات. قد لا تملك الصلاحيات الكافية على الخادم.",
                FormatDetails(ex),
                false);
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(
                false,
                "حدث خطأ غير متوقع أثناء إنشاء قاعدة البيانات.",
                FormatDetails(ex),
                false);
        }
    }

    public async Task<IReadOnlyList<string>> ListDatabasesAsync(string connectionString, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return Array.Empty<string>();

        SqlConnectionStringBuilder masterBuilder;
        try
        {
            masterBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                Pooling = false,
                ConnectTimeout = 8
            };
        }
        catch
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();

        try
        {
            await using var conn = new SqlConnection(masterBuilder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT name FROM sys.databases WHERE database_id > 4 AND state_desc = 'ONLINE' ORDER BY name";

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                if (!reader.IsDBNull(0))
                    list.Add(reader.GetString(0));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // الفشل يُعاد بقائمة فارغة — TestAsync يتولى عرض رسالة الخطأ الفعلية.
            return Array.Empty<string>();
        }

        return list;
    }

    private static string FormatDetails(Exception ex)
    {
        var detail = $"{ex.GetType().Name}: {ex.Message}";
        if (ex.InnerException is not null)
            detail += $"\n→ {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
        return detail;
    }

    /// <summary>
    /// Same whitelist as <c>BackupService.IsSafeDatabaseName</c>:
    /// letters / digits / underscore / dash / space, ≤ 128 chars.
    /// Keeps bracket-quoted identifiers safe.
    /// </summary>
    private static bool IsSafeDatabaseName(string name)
    {
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
