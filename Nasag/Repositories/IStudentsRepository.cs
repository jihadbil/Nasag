using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;
using Nasag.Services;

namespace Nasag.Repositories;

public enum StudentSortMode { Alphabetical, NewestFirst }

public interface IStudentsRepository
{
    Task<StudentListPage> SearchAsync(StudentsQuery query, CancellationToken ct = default);
    Task<StudentStats> GetStatsAsync(CancellationToken ct = default);
    Task<StudentEditorLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<StudentEditorPayload?> GetForEditAsync(int studentId, CancellationToken ct = default);
    Task<int> CreateAsync(StudentSaveModel model, CancellationToken ct = default);
    Task UpdateAsync(StudentSaveModel model, CancellationToken ct = default);
    Task SetStatusAsync(int studentId, StudentStatus status, CancellationToken ct = default);
    Task DeleteAsync(int studentId, CancellationToken ct = default);
    Task<bool> StudentNumberExistsAsync(string studentNumber, int? excludeId, CancellationToken ct = default);
    Task<string> NextStudentNumberAsync(CancellationToken ct = default);

    /// <summary>Returns every student row formatted for Excel export (no paging, no filtering).</summary>
    Task<IReadOnlyList<StudentExportRow>> GetAllForExportAsync(CancellationToken ct = default);

    /// <summary>Deletes every student + their guardians (cascade clean-up before a full replace import).</summary>
    Task DeleteAllStudentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Bulk-imports valid student rows. Returns the number of inserted records.
    /// Caller is responsible for validation and grade/section name → id resolution.
    /// </summary>
    Task<int> BulkInsertAsync(IReadOnlyList<StudentSaveModel> models, CancellationToken ct = default);
}

public sealed record StudentsQuery(
    string? Search,
    int? GradeId,
    int? SectionId,
    StudentStatus? Status,
    int Page,
    int PageSize,
    StudentSortMode Sort = StudentSortMode.Alphabetical);

public sealed record StudentListPage(
    IReadOnlyList<StudentRow> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record StudentRow(
    int Id,
    string StudentNumber,
    string FullName,
    Gender Gender,
    StudentStatus Status,
    string GradeName,
    string SectionName,
    string? Phone,
    string GuardianName,
    string? GuardianPhone);

public sealed record StudentStats(int Total, int Active, int Archived);

public sealed record StudentEditorLookups(
    IReadOnlyList<GradeOption> Grades,
    IReadOnlyList<SectionOption> Sections);

public sealed record GradeOption(int Id, string NameAr);
public sealed record SectionOption(int Id, string NameAr, int GradeId);

public sealed record StudentEditorPayload(
    int Id,
    string StudentNumber,
    string FullName,
    Gender Gender,
    DateTime BirthDate,
    string? NationalId,
    string? PhotoPath,
    byte[]? PhotoBytes,
    string? Phone,
    string? Address,
    string? Notes,
    DateTime EnrollmentDate,
    StudentStatus Status,
    int SectionId,
    int GuardianId,
    string GuardianFullName,
    GuardianRelation GuardianRelation,
    string? GuardianPhone,
    string? GuardianAltPhone,
    string? GuardianEmail,
    string? GuardianNationalId,
    string? GuardianOccupation,
    string? GuardianAddress);

public sealed class StudentSaveModel
{
    public int? Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Male;
    public DateTime BirthDate { get; set; } = new DateTime(2015, 1, 1);
    public string? NationalId { get; set; }
    public string? PhotoPath { get; set; }
    public byte[]? PhotoBytes { get; set; }

    /// <summary>
    /// True only when <see cref="PhotoBytes"/> represents a deliberate change
    /// (new upload or removal). For Update flows, false means "leave the
    /// existing photo column untouched".
    /// </summary>
    public bool UpdatePhoto { get; set; }

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public int SectionId { get; set; }

    public int? GuardianId { get; set; }
    public string GuardianFullName { get; set; } = string.Empty;
    public GuardianRelation GuardianRelation { get; set; } = GuardianRelation.Father;
    public string? GuardianPhone { get; set; }
    public string? GuardianAltPhone { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianNationalId { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddress { get; set; }
}
