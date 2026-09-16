using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Repositories;

public interface IMarksRepository
{
    Task<MarksLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<MarksSheet> GetSheetAsync(int sectionId, int subjectId, int examId, CancellationToken ct = default);
    Task SaveSheetAsync(int sectionId, int subjectId, int examId, IReadOnlyList<MarkSaveRow> rows, CancellationToken ct = default);
}

public sealed record MarksLookups(
    IReadOnlyList<MarksGradeOption> Grades,
    IReadOnlyList<MarksSectionOption> Sections,
    IReadOnlyList<MarksSubjectOption> Subjects,
    IReadOnlyList<MarksExamOption> Exams);

public sealed record MarksGradeOption(int Id, string NameAr);

public sealed record MarksSectionOption(
    int Id,
    string NameAr,
    int GradeId,
    string GradeName,
    int StudentCount)
{
    public string DisplayName => $"{NameAr} ({StudentCount})";
}

public sealed record MarksSubjectOption(
    int Id,
    string NameAr,
    int GradeId,
    decimal MaxMark,
    decimal PassMark);

public sealed record MarksExamOption(int Id, string NameAr, decimal Weight);

public sealed record MarksSheet(
    int SectionId,
    int SubjectId,
    int ExamId,
    decimal MaxMark,
    decimal PassMark,
    IReadOnlyList<MarksStudentRow> Rows);

public sealed record MarksStudentRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    decimal? Value,
    string? Notes,
    int? ExistingMarkId);

public sealed record MarkSaveRow(int StudentId, decimal? Value, string? Notes);
