using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Repositories;

/// <summary>
/// Read/write surface for the Subjects screen — list rows with mark-counts,
/// grade lookup options, and CRUD that validates name+grade uniqueness and
/// refuses to delete subjects that already have marks recorded against them.
/// </summary>
public interface ISubjectsRepository
{
    Task<IReadOnlyList<SubjectRow>> GetAllAsync(int? gradeId, string? search, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectGradeOption>> GetGradesAsync(CancellationToken ct = default);
    Task<int> CreateAsync(SubjectSaveModel model, CancellationToken ct = default);
    Task UpdateAsync(SubjectSaveModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<int> GetMarksCountAsync(int subjectId, CancellationToken ct = default);
}

public sealed record SubjectRow(
    int Id,
    string NameAr,
    int GradeId,
    string GradeName,
    decimal MaxMark,
    decimal PassMark,
    int MarksCount);

public sealed record SubjectGradeOption(int Id, string NameAr);

public sealed class SubjectSaveModel
{
    public int? Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int GradeId { get; set; }
    public decimal MaxMark { get; set; } = 100m;
    public decimal PassMark { get; set; } = 50m;
}
