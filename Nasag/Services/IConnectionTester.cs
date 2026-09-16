using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

public enum ConnectionTestStage { Reachability, DatabasePresent, Both }

/// <summary>
/// Outcome of a connection probe. All user-facing text is Arabic; the
/// <see cref="Details"/> field carries raw exception text (English ok) for
/// the "show details" toggle in the wizard.
/// </summary>
public sealed record ConnectionTestResult(
    bool Success,
    string Message,
    string? Details,
    bool DatabaseExists);

public interface IConnectionTester
{
    /// <summary>
    /// Pings the SQL Server using the given connection string by opening a
    /// short-lived connection against <c>master</c>, then checks whether the
    /// target database (from <c>InitialCatalog</c>) exists.
    /// </summary>
    Task<ConnectionTestResult> TestAsync(string connectionString, CancellationToken ct = default);

    /// <summary>
    /// Issues <c>CREATE DATABASE [name]</c> against <c>master</c> for the
    /// database named in the connection string's <c>InitialCatalog</c>.
    /// The name is whitelist-validated to keep bracket-quoting safe.
    /// </summary>
    Task<ConnectionTestResult> CreateDatabaseAsync(string connectionString, CancellationToken ct = default);

    /// <summary>
    /// يسرد قواعد البيانات الخاصة بالمستخدم (باستثناء master/model/msdb/tempdb)
    /// المتاحة عبر سلسلة الاتصال المعطاة. يتصل بـ <c>master</c> (يستنسخ
    /// <c>SqlConnectionStringBuilder</c>) لذا قاعدة البيانات الهدف قد لا تكون موجودة بعد.
    /// عند الفشل يعيد قائمة فارغة (لا يرمي).
    /// </summary>
    Task<IReadOnlyList<string>> ListDatabasesAsync(string connectionString, CancellationToken ct = default);
}
