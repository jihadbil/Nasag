using System;
using System.Threading.Tasks;
using Nasag.Licensing.License;
using NasaqVendor.Models;

namespace NasaqVendor.Services;

public sealed class IssueLicenseRequest
{
    public int CustomerId { get; set; }
    public LicenseEdition Edition { get; set; }
    public string[] MachineHashes { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAtUtc { get; set; }
    /// <summary>Final .naslic path on disk. The issuer writes here.</summary>
    public string TargetFilePath { get; set; } = "";
}

public sealed class IssueLicenseResult
{
    public LicenseRecord Record { get; set; } = new();
    public string FilePath { get; set; } = "";
}

public interface ILicenseIssuer
{
    Task<IssueLicenseResult> IssueAsync(IssueLicenseRequest req);
    Task<string> ReExportAsync(int licenseId, string newTargetPath);
}
