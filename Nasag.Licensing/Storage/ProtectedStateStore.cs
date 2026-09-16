using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace Nasag.Licensing.Storage;

/// <summary>
/// مخزن DPAPI آمن للحالة (الترخيص/التجربة) — نطاق المستخدم الحالي.
/// </summary>
public sealed class ProtectedStateStore
{
    private const string MutexName = @"Global\Nasaq.Licensing.State";
    private static readonly byte[] OptionalEntropy = new byte[]
    {
        0x4E, 0x61, 0x73, 0x61, 0x71, 0x2E, 0x4C, 0x69,
        0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 0x67, 0x2E,
        0x76, 0x31
    }; // "Nasaq.Licensing.v1"

    private readonly string _filePath;
    private readonly object _gate = new();

    public ProtectedStateStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("مسار ملف الحالة مطلوب.", nameof(filePath));
        _filePath = filePath;
    }

    public bool Exists()
    {
        try { return File.Exists(_filePath); }
        catch { return false; }
    }

    public byte[]? ReadProtected()
    {
        lock (_gate)
        {
            using var mutex = AcquireMutex();
            try
            {
                if (!File.Exists(_filePath)) return null;
                var encrypted = File.ReadAllBytes(_filePath);
                if (encrypted.Length == 0) return null;
                try
                {
                    return ProtectedData.Unprotect(encrypted, OptionalEntropy, DataProtectionScope.CurrentUser);
                }
                catch (CryptographicException)
                {
                    // تالف أو تم نسخه من حساب آخر — اعتبره مفقوداً.
                    return null;
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            finally
            {
                ReleaseMutex(mutex);
            }
        }
    }

    public void WriteProtected(byte[] plaintext)
    {
        if (plaintext is null)
            throw new ArgumentNullException(nameof(plaintext), "البيانات المراد حمايتها مطلوبة.");

        lock (_gate)
        {
            using var mutex = AcquireMutex();
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var encrypted = ProtectedData.Protect(plaintext, OptionalEntropy, DataProtectionScope.CurrentUser);
                var tmp = _filePath + ".tmp";
                File.WriteAllBytes(tmp, encrypted);

                // استبدال ذرّي عبر File.Replace أو Move.
                if (File.Exists(_filePath))
                {
                    try
                    {
                        File.Replace(tmp, _filePath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(_filePath);
                        File.Move(tmp, _filePath);
                    }
                }
                else
                {
                    File.Move(tmp, _filePath);
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("فشل تشفير ملف الحالة عبر DPAPI.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("فشل كتابة ملف الحالة على القرص.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("لا تتوفر صلاحيات لكتابة ملف الحالة.", ex);
            }
            finally
            {
                ReleaseMutex(mutex);
            }
        }
    }

    public void Delete()
    {
        lock (_gate)
        {
            using var mutex = AcquireMutex();
            try
            {
                if (File.Exists(_filePath)) File.Delete(_filePath);
            }
            catch { /* تجاهل أخطاء الحذف */ }
            finally
            {
                ReleaseMutex(mutex);
            }
        }
    }

    private static Mutex AcquireMutex()
    {
        var mutex = new Mutex(false, MutexName);
        try { mutex.WaitOne(TimeSpan.FromSeconds(10)); }
        catch (AbandonedMutexException) { /* وريث Mutex مهجور — تجاهل */ }
        return mutex;
    }

    private static void ReleaseMutex(Mutex mutex)
    {
        try { mutex.ReleaseMutex(); } catch { /* تجاهل */ }
    }
}
