using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using Nasag.Licensing;

namespace Nasag.Services.Licensing;

/// <summary>
/// تطبيق Velopack مع كشف تلقائي لنوع المصدر:
///   • URL بـ http(s):// → SimpleWebSource (موزّع عبر HTTP/HTTPS).
///   • URL يحوي github.com → GithubSource (إصدارات GitHub Releases).
///   • أي شيء آخر → SimpleFileSource (مجلد محلي/شبكة UNC).
/// مكان المصدر يُحفَظ في تفضيلات المستخدم؛ القيمة الافتراضية ‎%LOCALAPPDATA%\Nasaq\Updates‎.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private readonly IUserPreferencesService _prefs;
    private readonly object _gate = new();
    private UpdateManager? _manager;
    private UpdateInfo? _pendingInfo;
    private string _sourcePath;

    public UpdateService(IUserPreferencesService prefs)
    {
        _prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
        _sourcePath = ResolveInitialSource();
        TryBuildManager();
    }

    public string CurrentVersion
    {
        get
        {
            try
            {
                var v = _manager?.CurrentVersion;
                if (v is not null) return v.ToString();
            }
            catch
            {
                // قد يفشل خارج البيئة المُثبَّتة (Velopack).
            }

            var assembly = typeof(UpdateService).Assembly;
            var info = assembly.GetName().Version;
            return info?.ToString(3) ?? "1.14.0";
        }
    }

    public DateTime? LastCheckedUtc { get; private set; }

    public string UpdateSource => _sourcePath;

    public UpdateSourceKind SourceKind => DetectKind(_sourcePath);

    public void SetUpdateSource(string sourceLocation)
    {
        if (string.IsNullOrWhiteSpace(sourceLocation))
            throw new ArgumentException("مصدر التحديثات مطلوب (مجلد محلي أو رابط HTTP أو مستودع GitHub).", nameof(sourceLocation));

        lock (_gate)
        {
            _sourcePath = sourceLocation.Trim();
            _prefs.Current.UpdateSourceFolder = _sourcePath;
            try { _prefs.Save(); } catch { /* تجاهل */ }
            TryBuildManager();
            _pendingInfo = null;
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        UpdateManager? mgr;
        lock (_gate) { mgr = _manager; }

        if (mgr is null)
            return new UpdateCheckResult(false, null, null);

        try
        {
            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            LastCheckedUtc = DateTime.UtcNow;

            if (info is null || info.TargetFullRelease is null)
            {
                lock (_gate) { _pendingInfo = null; }
                return new UpdateCheckResult(false, null, null);
            }

            lock (_gate) { _pendingInfo = info; }
            var version = info.TargetFullRelease.Version?.ToString();
            return new UpdateCheckResult(true, version, null);
        }
        catch (Exception)
        {
            // فشل الشبكة/المجلد. نُعيد "لا تحديث" بدل الرمي.
            LastCheckedUtc = DateTime.UtcNow;
            return new UpdateCheckResult(false, null, null);
        }
    }

    public async Task DownloadAsync(IProgress<int>? progress, CancellationToken ct = default)
    {
        UpdateManager? mgr;
        UpdateInfo? info;
        lock (_gate)
        {
            mgr = _manager;
            info = _pendingInfo;
        }
        if (mgr is null || info is null)
            throw new InvalidOperationException("لا يوجد تحديث جاهز للتنزيل. شغّل الفحص أولاً.");

        await mgr.DownloadUpdatesAsync(info, p => progress?.Report(p), ct).ConfigureAwait(false);
    }

    public void ApplyAndRestart()
    {
        UpdateManager? mgr;
        UpdateInfo? info;
        lock (_gate)
        {
            mgr = _manager;
            info = _pendingInfo;
        }
        if (mgr is null || info is null)
            throw new InvalidOperationException("لا يوجد تحديث جاهز للتطبيق.");

        mgr.ApplyUpdatesAndRestart(info);
    }

    // ───────── داخلي ─────────

    /// <summary>المستودع الرسمي لإصدارات نَسَق.</summary>
    public const string DefaultGithubRepo = "https://github.com/engsafaaj/Nasag";

    private string ResolveInitialSource()
    {
        var fromPrefs = _prefs.Current.UpdateSourceFolder;
        if (!string.IsNullOrWhiteSpace(fromPrefs))
        {
            // مهاجرة الـ default المحلي القديم %LOCALAPPDATA%\Nasaq\Updates إلى مستودع GitHub الرسمي
            // إذا لم يُغيّره المستخدم صراحةً (يكتشف ذلك بأن القيمة المحفوظة تساوي المسار المحلي الافتراضي).
            var legacyDefault = Path.Combine(PathProvider.NasaqLocalAppData, "Updates");
            if (string.Equals(fromPrefs.Trim(), legacyDefault, StringComparison.OrdinalIgnoreCase))
                return DefaultGithubRepo;
            return fromPrefs;
        }

        // أول تشغيل: المستودع الرسمي على GitHub.
        return DefaultGithubRepo;
    }

    private void TryBuildManager()
    {
        try
        {
            IUpdateSource src;
            var kind = DetectKind(_sourcePath);

            switch (kind)
            {
                case UpdateSourceKind.Github:
                    // مستودع GitHub: نأخذ النسخ من إصدارات الـ Releases مباشرة.
                    src = new GithubSource(_sourcePath, accessToken: null, prerelease: false);
                    break;

                case UpdateSourceKind.WebUrl:
                    src = new SimpleWebSource(_sourcePath);
                    break;

                default:
                    // مجلد محلي/شبكي. نُنشئه إن لم يوجد لتلافي رمي عند الانطلاق.
                    try { Directory.CreateDirectory(_sourcePath); } catch { /* تجاهل */ }
                    src = new SimpleFileSource(new DirectoryInfo(_sourcePath));
                    break;
            }

            _manager = new UpdateManager(src);
        }
        catch
        {
            _manager = null;
        }
    }

    private static UpdateSourceKind DetectKind(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return UpdateSourceKind.Folder;
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)) return UpdateSourceKind.Folder;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return UpdateSourceKind.Folder;
        if (uri.Host.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) >= 0)
            return UpdateSourceKind.Github;
        return UpdateSourceKind.WebUrl;
    }
}

public enum UpdateSourceKind
{
    Folder,
    WebUrl,
    Github,
}
