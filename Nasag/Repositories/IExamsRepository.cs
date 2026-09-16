using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Repositories;

public interface IExamsRepository
{
    Task<IReadOnlyList<ExamRow>> GetAllAsync(int? academicYearId, string? search, CancellationToken ct = default);
    Task<IReadOnlyList<ExamYearOption>> GetYearsAsync(CancellationToken ct = default);
    Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default);
    Task<int> CreateAsync(ExamSaveModel model, CancellationToken ct = default);
    Task UpdateAsync(ExamSaveModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed record ExamRow(
    int Id,
    string NameAr,
    int AcademicYearId,
    string AcademicYearName,
    decimal Weight,
    int MarksCount);

public sealed record ExamYearOption(int Id, string NameAr, bool IsActive);

public sealed class ExamSaveModel
{
    public int? Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public decimal Weight { get; set; } = 1m;
}
