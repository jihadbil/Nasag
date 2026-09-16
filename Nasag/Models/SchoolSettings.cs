namespace Nasag.Models;

public class SchoolSettings
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Legacy file-path column kept for backward-compatibility with earlier seed data.
    /// New logos are stored as bytes in <see cref="LogoBytes"/> and persisted in the DB.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// School logo bytes (image binary stored directly in the database as varbinary(max)).
    /// All school data — including the logo — lives in the DB per the project's
    /// "Data Storage Rule (DB-only)" (see AI_INSTRUCTIONS.md).
    /// </summary>
    public byte[]? LogoBytes { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? PrincipalName { get; set; }

    public int? CurrentAcademicYearId { get; set; }
    public AcademicYear? CurrentAcademicYear { get; set; }
}
