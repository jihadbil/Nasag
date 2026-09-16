using System;
using Nasag.Licensing.License;

namespace NasaqVendor.Models;

public sealed class LicenseRecord
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public LicenseEdition Edition { get; set; }
    public string FeaturesJson { get; set; } = "[]";
    public string MachineHashesJson { get; set; } = "[]";
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string LicenseFilePath { get; set; } = "";
    public bool Revoked { get; set; }

    // joined display columns (not persisted)
    public string CustomerName { get; set; } = "";
    public string CustomerCode { get; set; } = "";
}
