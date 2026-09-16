namespace Nasag.Services;

/// <summary>
/// مخزن مؤقّت ومحمي (DPAPI) لبيانات المدير القادمة من معالج الإعداد.
/// يُحفظ في ملف بنطاق المستخدم الحالي ويُستهلك مرة واحدة فقط عند Seed.
/// </summary>
public interface IPendingAdminSetupStore
{
    /// <summary>Path to the encrypted file (%LOCALAPPDATA%\Nasaq\pending-admin.dat).</summary>
    string FilePath { get; }

    /// <summary>True if a pending admin payload exists on disk.</summary>
    bool HasPending { get; }

    /// <summary>Persists the admin payload encrypted with DPAPI (per-user scope).</summary>
    void Save(PendingAdminSetup setup);

    /// <summary>Reads and DELETES the file in a single operation. Returns null if no pending payload.</summary>
    PendingAdminSetup? ReadAndClear();
}
