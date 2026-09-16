using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Repositories;

public interface IClassesRepository
{
    Task<IReadOnlyList<GradeRow>> GetGradesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SectionRow>> GetSectionsForGradeAsync(int gradeId, CancellationToken ct = default);
    Task<IReadOnlyList<SectionStudentRow>> GetStudentsForSectionAsync(int sectionId, CancellationToken ct = default);
    Task<ClassesStats> GetStatsAsync(CancellationToken ct = default);
    Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default);

    Task<int> CreateGradeAsync(GradeSaveModel model, CancellationToken ct = default);
    Task UpdateGradeAsync(GradeSaveModel model, CancellationToken ct = default);
    Task DeleteGradeAsync(int gradeId, CancellationToken ct = default);
    Task<GradeDependencyCounts> GetGradeDependencyCountsAsync(int gradeId, CancellationToken ct = default);

    Task<int> CreateSectionAsync(SectionSaveModel model, CancellationToken ct = default);
    Task UpdateSectionAsync(SectionSaveModel model, CancellationToken ct = default);
    Task DeleteSectionAsync(int sectionId, CancellationToken ct = default);
    Task<SectionDependencyCounts> GetSectionDependencyCountsAsync(int sectionId, CancellationToken ct = default);

    /// <summary>
    /// Moves a student to a different section. Validates the target section
    /// belongs to a real grade, exists in the current year, and has remaining
    /// capacity (throws <see cref="System.InvalidOperationException"/> otherwise).
    /// </summary>
    Task MoveStudentAsync(int studentId, int targetSectionId, CancellationToken ct = default);

    /// <summary>Sections grouped by grade for the move dialog. Includes count + capacity.</summary>
    Task<IReadOnlyList<MoveTargetSection>> GetMoveTargetsAsync(int? excludeStudentId, CancellationToken ct = default);
}

public sealed record GradeRow(int Id, string NameAr, GradeLevel Level, int SortOrder, int SectionCount, int StudentCount);

public sealed record SectionRow(
    int Id,
    int GradeId,
    string GradeName,
    string NameAr,
    int Capacity,
    int StudentCount)
{
    public int Remaining => Capacity - StudentCount;
    public bool IsOverCapacity => StudentCount > Capacity;
}

public sealed record SectionStudentRow(
    int Id,
    string StudentNumber,
    string FullName,
    Gender Gender,
    StudentStatus Status,
    string? Phone);

public sealed record ClassesStats(int GradeCount, int SectionCount, int StudentCount);

public sealed record GradeDependencyCounts(int SectionCount, int StudentCount, int SubjectCount);
public sealed record SectionDependencyCounts(int StudentCount);

public sealed class GradeSaveModel
{
    public int? Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public GradeLevel Level { get; set; } = GradeLevel.Primary;
    public int SortOrder { get; set; }
}

public sealed class SectionSaveModel
{
    public int? Id { get; set; }
    public int GradeId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int Capacity { get; set; } = 30;
}

public sealed record MoveTargetSection(
    int Id,
    int GradeId,
    string GradeName,
    string NameAr,
    int Capacity,
    int StudentCount)
{
    public string Display => $"{GradeName} — {NameAr} ({StudentCount}/{Capacity})";
}
