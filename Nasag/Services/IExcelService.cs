using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

/// <summary>Single row read from a student Excel file. All fields are strings; conversion happens in the wizard.</summary>
public sealed class StudentImportRow
{
    public string? StudentNumber { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? GradeName { get; set; }
    public string? SectionName { get; set; }
    public string? EnrollmentDate { get; set; }
    public string? Address { get; set; }
    public string? GuardianFullName { get; set; }
    public string? GuardianRelation { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianAltPhone { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianNationalId { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddress { get; set; }
    public string? Notes { get; set; }

    /// <summary>1-based source row number for error reporting.</summary>
    public int RowNumber { get; set; }
}

public sealed class StudentExportRow
{
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string EnrollmentDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string GuardianFullName { get; set; } = string.Empty;
    public string GuardianRelation { get; set; } = string.Empty;
    public string? GuardianPhone { get; set; }
    public string? GuardianAltPhone { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianNationalId { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddress { get; set; }
    public string? Notes { get; set; }
}

public interface IExcelService
{
    /// <summary>Writes a styled .xlsx file (headers, freeze pane, autofit). Throws on IO/encoding failures.</summary>
    Task ExportStudentsAsync(string filePath, IReadOnlyList<StudentExportRow> rows, CancellationToken ct = default);

    /// <summary>Reads a .xlsx file and returns one row per data row (skips the header). All cells coerced to strings.</summary>
    Task<IReadOnlyList<StudentImportRow>> ReadStudentsAsync(string filePath, CancellationToken ct = default);

    /// <summary>Writes a blank import template (.xlsx) with the canonical Arabic headers.</summary>
    Task WriteTemplateAsync(string filePath, CancellationToken ct = default);

    // ---------- Phase 11: Reports exports ----------

    /// <summary>Writes a styled students report (filtered list) to .xlsx.</summary>
    Task ExportStudentsReportAsync(string filePath, Nasag.Repositories.StudentsReportResult result, CancellationToken ct = default);

    /// <summary>Writes a styled attendance period report to .xlsx (one row per student with P/A/L/E counts + %).</summary>
    Task ExportAttendanceReportAsync(string filePath, Nasag.Repositories.AttendanceReportResult result, CancellationToken ct = default);

    /// <summary>Writes a styled marks/results report to .xlsx (dynamic subject columns + totals + grade).</summary>
    Task ExportMarksReportAsync(string filePath, Nasag.Repositories.MarksReportResult result, CancellationToken ct = default);

    /// <summary>Writes a styled fees summary report to .xlsx (per-student total/paid/remaining/overdue/status).</summary>
    Task ExportFeesReportAsync(string filePath, Nasag.Repositories.FeesReportResult result, CancellationToken ct = default);
}
