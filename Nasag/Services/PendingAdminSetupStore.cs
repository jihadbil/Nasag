using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nasag.Services;

/// <summary>
/// تنفيذ <see cref="IPendingAdminSetupStore"/> يحفظ البيانات في ملف ثنائي
/// مشفّر بـ DPAPI (نطاق المستخدم الحالي فقط) داخل
/// <c>%LOCALAPPDATA%\Nasaq\pending-admin.dat</c>.
/// مأمون من حيث المزامنة عبر <c>lock</c>، والقراءة تستهلك الملف وتحذفه دوماً.
/// </summary>
public sealed class PendingAdminSetupStore : IPendingAdminSetupStore
{
    private const string FolderName = "Nasaq";
    private const string FileName = "pending-admin.dat";

    private readonly object _gate = new();
    private readonly string _filePath;

    public PendingAdminSetupStore()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _filePath = Path.Combine(baseDir, FolderName, FileName);
    }

    public string FilePath => _filePath;

    public bool HasPending
    {
        get
        {
            lock (_gate)
            {
                return File.Exists(_filePath);
            }
        }
    }

    public void Save(PendingAdminSetup setup)
    {
        if (setup is null) throw new ArgumentNullException(nameof(setup));

        lock (_gate)
        {
            // التأكد من وجود المجلد قبل الكتابة.
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 1) Serialize → 2) DPAPI Protect → 3) Write
            var json = JsonSerializer.Serialize(setup);
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = ProtectedData.Protect(plain, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_filePath, cipher);
        }
    }

    public PendingAdminSetup? ReadAndClear()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath)) return null;

            byte[] cipher;
            try
            {
                cipher = File.ReadAllBytes(_filePath);
            }
            catch
            {
                // تعذّر القراءة — احذف الملف الفاسد بصمت وارجع null.
                TryDelete();
                return null;
            }

            byte[] plain;
            try
            {
                plain = ProtectedData.Unprotect(cipher, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            }
            catch
            {
                TryDelete();
                return null;
            }

            PendingAdminSetup? payload;
            try
            {
                var json = Encoding.UTF8.GetString(plain);
                payload = JsonSerializer.Deserialize<PendingAdminSetup>(json);
            }
            catch
            {
                TryDelete();
                return null;
            }

            // نجح كل شيء أو فشل تحويل JSON — في الحالتين نحذف الملف
            // لأن الـ contract يقول "يستهلك" البيانات.
            TryDelete();
            return payload;
        }
    }

    private void TryDelete()
    {
        try { if (File.Exists(_filePath)) File.Delete(_filePath); }
        catch { /* تجاهل — لا يوجد ما يمكن فعله إذا فشل الحذف. */ }
    }
}
