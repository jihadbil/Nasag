using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Repositories;

public interface IReportsRepository
{
    Task<ReportLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<SchoolHeaderInfo> GetSchoolHeaderAsync(CancellationToken ct = default);

    Task<StudentsReportResult> GetStudentsReportAsync(StudentsReportQuery query, CancellationToken ct = default);
    Task<AttendanceReportResult> GetAttendanceReportAsync(AttendanceReportQuery query, CancellationToken ct = default);
    Task<MarksReportResult> GetMarksReportAsync(MarksReportQuery query, CancellationToken ct = default);
    Task<FeesReportResult> GetFeesReportAsync(FeesReportQuery query, CancellationToken ct = default);
}

// ---------- Shared lookup DTOs ----------

public sealed record ReportLookups(
    IReadOnlyList<ReportGradeOption> Grades,
    IReadOnlyList<ReportSectionOption> Sections,
    IReadOnlyList<ReportExamOption> Exams);

public sealed record ReportGradeOption(int Id, string NameAr);

public sealed record ReportSectionOption(int Id, int GradeId, string NameAr, string GradeName)
{
    public string DisplayName => $"{GradeName} — {NameAr}";
}

public sealed record ReportExamOption(int Id, string NameAr, double Weight);

// ---------- Students report ----------

public sealed record StudentsReportQuery(int? GradeId, int? SectionId, StudentStatus? Status);

public sealed record StudentsReportResult(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    string AcademicYearAr,
    string? GradeNameAr,
    string? SectionNameAr,
    string StatusLabelAr,
    DateTime GeneratedAt,
    IReadOnlyList<StudentReportRow> Rows);

public sealed record StudentReportRow(
    int Id,
    string StudentNumber,
    string FullName,
    string GenderAr,
    string GradeNameAr,
    string SectionNameAr,
    string? GuardianNameAr,
    string? GuardianPhone,
    string StatusAr,
    DateTime EnrollmentDate);

// ---------- Attendance report (period) ----------

public sealed record AttendanceReportQuery(DateTime DateFrom, DateTime DateTo, int? GradeId, int? SectionId);

public sealed record AttendanceReportResult(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    string AcademicYearAr,
    DateTime DateFrom,
    DateTime DateTo,
    string? GradeNameAr,
    string? SectionNameAr,
    DateTime GeneratedAt,
    IReadOnlyList<AttendanceReportRow> Rows,
    AttendanceReportTotals Totals);

public sealed record AttendanceReportRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    string GradeNameAr,
    string SectionNameAr,
    int Present,
    int Absent,
    int Late,
    int Excused,
    int RecordedDays,
    double AttendancePercentage);

public sealed record AttendanceReportTotals(
    int StudentCount,
    int RecordedDays,
    int TotalPresent,
    int TotalAbsent,
    int TotalLate,
    int TotalExcused,
    double AveragePercentage);

// ---------- Marks report ----------

public sealed record MarksReportQuery(int GradeId, int SectionId, int? ExamId);

public sealed record MarksReportResult(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    string AcademicYearAr,
    string GradeNameAr,
    string SectionNameAr,
    string ExamNameAr,
    bool IsAllExamsAggregate,
    DateTime GeneratedAt,
    IReadOnlyList<MarksReportColumn> Columns,
    IReadOnlyList<MarksReportRow> Rows,
    MarksReportTotals Totals);

public sealed record MarksReportColumn(int SubjectId, string SubjectNameAr, int MaxMark, int PassMark);

public sealed record MarksReportRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    IReadOnlyList<MarksReportCell> Cells,
    decimal Total,
    decimal MaxTotal,
    double Percentage,
    string GradeLabelAr,
    string StatusAr,
    bool IsPassed,
    bool IsPending);

public sealed record MarksReportCell(int SubjectId, decimal? Mark, bool IsPass, bool IsAbsent);

public sealed record MarksReportTotals(
    int StudentCount,
    int PassedCount,
    int FailedCount,
    int PendingCount,
    double AveragePercentage,
    double HighestPercentage,
    double LowestPercentage);

// ---------- Fees report ----------

public enum FeesReportStatusFilter
{
    All = 0,
    FullyPaid = 1,
    PartiallyPaid = 2,
    Unpaid = 3,
    HasOverdue = 4
}

public sealed record FeesReportQuery(int? GradeId, int? SectionId, FeesReportStatusFilter Status);

public sealed record FeesReportResult(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    string AcademicYearAr,
    string? GradeNameAr,
    string? SectionNameAr,
    FeesReportStatusFilter Status,
    string StatusLabelAr,
    DateTime GeneratedAt,
    IReadOnlyList<FeesReportRow> Rows,
    FeesReportTotals Totals);

public sealed record FeesReportRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    string GradeNameAr,
    string SectionNameAr,
    string? FeePlanNameAr,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    int OverdueCount,
    string StatusAr);

public sealed record FeesReportTotals(
    int StudentCount,
    decimal TotalAmount,
    decimal TotalPaid,
    decimal TotalRemaining,
    int TotalOverdue);
