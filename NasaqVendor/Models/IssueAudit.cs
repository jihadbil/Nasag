using System;

namespace NasaqVendor.Models;

public sealed class IssueAudit
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public string Action { get; set; } = "";
    public DateTime AtUtc { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}
