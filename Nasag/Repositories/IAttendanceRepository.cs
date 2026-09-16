using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Repositories;

public interface IAttendanceRepository
{
    Task<AttendanceLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<AttendanceSheet> GetAttendanceSheetAsync(int sectionId, DateTime date, CancellationToken ct = default);
    Task SaveAttendanceSheetAsync(int sectionId, DateTime date, IReadOnlyList<AttendanceSaveRow> rows, CancellationToken ct = default);
}

public sealed record AttendanceLookups(
    IReadOnlyList<AttendanceGradeOption> Grades,
    IReadOnlyList<AttendanceSectionOption> Sections);

public sealed record AttendanceGradeOption(int Id, string NameAr);

public sealed record AttendanceSectionOption(
    int Id,
    string NameAr,
    int GradeId,
    string GradeName,
    int StudentCount)
{
    public string DisplayName => $"{NameAr} ({StudentCount})";
}

public sealed record AttendanceSheet(
    int SectionId,
    DateTime Date,
    IReadOnlyList<AttendanceStudentRow> Rows);

public sealed record AttendanceStudentRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    AttendanceStatus Status,
    string? Notes,
    int? ExistingRecordId);

public sealed record AttendanceSaveRow(
    int StudentId,
    AttendanceStatus Status,
    string? Notes);
