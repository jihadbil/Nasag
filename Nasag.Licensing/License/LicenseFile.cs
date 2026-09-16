using System;

namespace Nasag.Licensing.License;

/// <summary>
/// DTO لملف ترخيص .naslic بتسلسل JSON.
/// </summary>
public sealed class LicenseFile
{
    public int V { get; set; } = 1;
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";

    /// <summary>تجزئات SHA-256 لمكوّنات بصمة الجهاز الخمسة (Hex).</summary>
    public string[] MachineHashes { get; set; } = Array.Empty<string>();

    public DateTime IssuedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public LicenseEdition Edition { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();

    /// <summary>توقيع ECDSA-P256 على الـ Canonical JSON بدون هذا الحقل، بصيغة Base64.</summary>
    public string Signature { get; set; } = "";
}
