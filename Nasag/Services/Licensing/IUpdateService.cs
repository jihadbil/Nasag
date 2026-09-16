using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services.Licensing;

/// <summary>
/// نتيجة فحص التحديثات.
/// </summary>
public sealed record UpdateCheckResult(bool HasUpdate, string? NewVersion, string? Notes);

/// <summary>
/// خدمة تحديثات نَسَق عبر Velopack — تكشف نوع المصدر تلقائياً:
/// مجلد محلي/شبكة، رابط HTTP/HTTPS، أو مستودع GitHub Releases.
/// </summary>
public interface IUpdateService
{
    string CurrentVersion { get; }
    DateTime? LastCheckedUtc { get; }

    /// <summary>المصدر الحالي المُعتمد (مسار مجلد، أو URL، أو رابط GitHub).</summary>
    string UpdateSource { get; }

    /// <summary>نوع المصدر المُكتشف من القيمة الحالية.</summary>
    UpdateSourceKind SourceKind { get; }

    /// <summary>
    /// تحديث مصدر التحديثات. يقبل ثلاثة أنواع:
    /// مسار مجلد محلي (C:\... أو \\server\share)، أو رابط HTTP/HTTPS عام،
    /// أو رابط مستودع GitHub بصيغة https://github.com/owner/repo.
    /// يُحفظ في تفضيلات المستخدم.
    /// </summary>
    void SetUpdateSource(string sourceLocation);

    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);
    Task DownloadAsync(IProgress<int>? progress, CancellationToken ct = default);
    void ApplyAndRestart();
}
