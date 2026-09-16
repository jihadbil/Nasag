using System;
using System.Linq;
using Nasag.Licensing.Cryptography;

namespace Nasag.Licensing.License;

/// <summary>
/// تحقق من صلاحية ترخيص: توقيع → انتهاء صلاحية → تطابق الجهاز (3-of-5).
/// </summary>
public sealed class LicenseValidator
{
    private readonly byte[] _publicKey;

    public LicenseValidator(byte[] publicKey)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("المفتاح العام مطلوب لإنشاء مُحقّق الترخيص.", nameof(publicKey));
        _publicKey = (byte[])publicKey.Clone();
    }

    /// <summary>
    /// تحقق من الترخيص باستخدام تجزئات الجهاز الحالية.
    /// </summary>
    public LicenseStatus Validate(LicenseFile? license, string[] currentMachineHashes)
    {
        if (license is null)
            return new LicenseStatus.Missing();

        // 1) التوقيع
        if (string.IsNullOrWhiteSpace(license.Signature))
            return new LicenseStatus.InvalidSignature("التوقيع الرقمي مفقود.");

        byte[] signatureBytes;
        try { signatureBytes = Convert.FromBase64String(license.Signature); }
        catch (FormatException)
        {
            return new LicenseStatus.InvalidSignature("صيغة التوقيع غير صالحة.");
        }

        byte[] payload;
        try { payload = LicenseSerializer.SerializeForSigning(license); }
        catch (Exception ex)
        {
            return new LicenseStatus.InvalidSignature($"تعذّر إعادة تسلسل بيانات الترخيص للتحقّق: {ex.Message}");
        }

        var sigOk = EcdsaSigner.Verify(payload, signatureBytes, _publicKey);
        if (!sigOk)
            return new LicenseStatus.InvalidSignature("التوقيع الرقمي للترخيص غير صحيح.");

        // 2) الانتهاء
        if (license.ExpiresAtUtc.HasValue)
        {
            var now = DateTime.UtcNow;
            if (now > license.ExpiresAtUtc.Value)
                return new LicenseStatus.Expired(license, "انتهت صلاحية الترخيص.");
        }

        // 3) تطابق الجهاز (3-of-5)
        var licensedHashes = license.MachineHashes ?? Array.Empty<string>();
        if (currentMachineHashes is null || currentMachineHashes.Length == 0)
            return new LicenseStatus.MachineMismatch(license, 0);

        int matches = CountMatches(currentMachineHashes, licensedHashes);
        if (matches < 3)
            return new LicenseStatus.MachineMismatch(license, matches);

        return new LicenseStatus.Activated(license);
    }

    private static int CountMatches(string[] current, string[] licensed)
    {
        int matches = 0;
        int len = Math.Min(current.Length, licensed.Length);
        for (int i = 0; i < len; i++)
        {
            var a = current[i] ?? "";
            var b = licensed[i] ?? "";
            if (a.Length == 0 || b.Length == 0) continue;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                matches++;
        }
        return matches;
    }
}
