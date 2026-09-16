using System;
using System.Security.Cryptography;
using System.Text;

namespace Nasag.Services;

public sealed class UserPreferences
{
    public string? RememberedUsername { get; set; }
    public bool StudentsSortAlphabetically { get; set; } = true;
    public int StudentsPageSize { get; set; } = 20;

    /// <summary>
    /// Phase 12 — remembered target folder for the next Backup &amp; Restore session.
    /// Null means "use the default" (<c>%LOCALAPPDATA%\Nasaq\Backups</c>). Per-machine
    /// like the rest of <see cref="UserPreferences"/>; not part of the school DB.
    /// </summary>
    public string? BackupFolder { get; set; }

    /// <summary>
    /// Phase 14 — مجلد مصدر تحديثات Velopack (محلي أو مشاركة شبكية).
    /// عند القيمة null تُستخدم القيمة الافتراضية <c>%LOCALAPPDATA%\Nasaq\Updates</c>.
    /// </summary>
    public string? UpdateSourceFolder { get; set; }

    /// <summary>
    /// كلمة المرور المحفوظة عند تفعيل «تذكّرني» — مُشفَّرة بـ DPAPI
    /// (CurrentUser scope) ثم Base64. لا تُخزَّن أبداً بنص واضح.
    /// </summary>
    public string? RememberedPasswordProtected { get; set; }

    /// <summary>يحدّد قيمة كلمة المرور المحفوظة عبر تشفيرها بـ DPAPI.</summary>
    public void SetRememberedPassword(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            RememberedPasswordProtected = null;
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        RememberedPasswordProtected = Convert.ToBase64String(protectedBytes);
    }

    /// <summary>يعيد كلمة المرور المحفوظة بنص واضح، أو null إن لم توجد أو فسدت.</summary>
    public string? GetRememberedPassword()
    {
        if (string.IsNullOrEmpty(RememberedPasswordProtected)) return null;
        try
        {
            var protectedBytes = Convert.FromBase64String(RememberedPasswordProtected);
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // فاسد أو من جلسة Windows مختلفة → نتعامل كأنه غير محفوظ.
            return null;
        }
    }
}

public interface IUserPreferencesService
{
    UserPreferences Current { get; }
    void Save();
    void Reload();
}
