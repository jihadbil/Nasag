using System.Threading;
using System.Threading.Tasks;
using Nasag.Repositories;

namespace Nasag.Services.Reports;

/// <summary>
/// Programmatic PDF export for Phase 11 reports. Implementations must
/// produce RTL Arabic PDFs using Tajawal font, matching the in-app
/// FlowDocument preview as closely as possible.
/// </summary>
public interface IReportPdfService
{
    Task SaveStudentsAsync(string filePath, StudentsReportResult result, CancellationToken ct = default);
    Task SaveAttendanceAsync(string filePath, AttendanceReportResult result, CancellationToken ct = default);
    Task SaveMarksAsync(string filePath, MarksReportResult result, CancellationToken ct = default);
    Task SaveFeesAsync(string filePath, FeesReportResult result, CancellationToken ct = default);
}
