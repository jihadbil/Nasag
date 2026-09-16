using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Services;

namespace Nasag.Repositories;

public interface IResultsRepository
{
    Task<ResultsLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentMarksInput>> GetStudentInputsAsync(int sectionId, int academicYearId, CancellationToken ct = default);
}

public sealed record ResultsLookups(
    IReadOnlyList<ResultsGradeOption> Grades,
    IReadOnlyList<ResultsSectionOption> Sections,
    IReadOnlyList<ResultsYearOption> Years);

public sealed record ResultsGradeOption(int Id, string NameAr);

public sealed record ResultsSectionOption(int Id, string NameAr, int GradeId, string GradeName, int AcademicYearId);

public sealed record ResultsYearOption(int Id, string NameAr, bool IsActive);
