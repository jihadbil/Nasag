namespace Nasag.Licensing.License;

/// <summary>
/// حالة الترخيص الناتجة عن التحقّق.
/// </summary>
public abstract record LicenseStatus
{
    public sealed record Trial(int DaysRemaining) : LicenseStatus;
    public sealed record Activated(LicenseFile License) : LicenseStatus;
    public sealed record Expired(LicenseFile? License, string Reason) : LicenseStatus;
    public sealed record TamperedClock(string Reason) : LicenseStatus;
    public sealed record MachineMismatch(LicenseFile License, int MatchCount) : LicenseStatus;
    public sealed record InvalidSignature(string Reason) : LicenseStatus;
    public sealed record Missing() : LicenseStatus;
}
