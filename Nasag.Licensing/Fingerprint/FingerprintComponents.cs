namespace Nasag.Licensing.Fingerprint;

/// <summary>
/// مكوّنات بصمة الجهاز الخمسة الخام.
/// </summary>
public sealed record FingerprintComponents(
    string Cpu,
    string Board,
    string Bios,
    string MachineGuid,
    string Mac);
