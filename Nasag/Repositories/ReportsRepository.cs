using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;
using Nasag.Services;

namespace Nasag.Repositories;

public sealed class ReportsRepository : IReportsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly IResultsCalculator _resultsCalculator;

    public ReportsRepository(
        IDbContextFactory<NasaqDbContext> factory,
        IResultsCalculator resultsCalculator)
    {
        _factory = factory;
        _resultsCalculator = resultsCalculator;
    }

    // -------------------- Lookups & Header --------------------

    public async Task<ReportLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new ReportGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = yearId.HasValue
            ? await ctx.Sections
                .AsNoTracking()
                .Where(s => s.AcademicYearId == yearId.Value)
                .OrderBy(s => s.Grade.SortOrder)
                .ThenBy(s => s.Grade.NameAr)
                .ThenBy(s => s.NameAr)
                .Select(s => new ReportSectionOption(s.Id, s.GradeId, s.NameAr, s.Grade.NameAr))
                .ToListAsync(ct)
                .ConfigureAwait(false)
            : new List<ReportSectionOption>();

        var exams = yearId.HasValue
            ? await ctx.Exams
                .AsNoTracking()
                .Where(e => e.AcademicYearId == yearId.Value)
                .OrderBy(e => e.NameAr)
                .Select(e => new ReportExamOption(e.Id, e.NameAr, (double)e.Weight))
                .ToListAsync(ct)
                .ConfigureAwait(false)
            : new List<ReportExamOption>();

        return new ReportLookups(grades, sections, exams);
    }

    public async Task<SchoolHeaderInfo> GetSchoolHeaderAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var info = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => new SchoolHeaderInfo(s.NameAr, s.Address, s.Phone))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return info ?? new SchoolHeaderInfo("نَسَق لإدارة المدارس", null, null);
    }

    // -------------------- Students Report --------------------

    public async Task<StudentsReportResult> GetStudentsReportAsync(StudentsReportQuery query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var header = await GetHeaderTupleAsync(ctx, ct).ConfigureAwait(false);

        var q = ctx.Students.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(s => s.Status == query.Status.Value);

        if (query.GradeId.HasValue)
            q = q.Where(s => s.Section.GradeId == query.GradeId.Value);

        if (query.SectionId.HasValue)
            q = q.Where(s => s.SectionId == query.SectionId.Value);

        var rows = await q
            .OrderBy(s => s.Section.Grade.SortOrder)
            .ThenBy(s => s.Section.NameAr)
            .ThenBy(s => s.FullName)
            .Select(s => new
            {
                s.Id,
                s.StudentNumber,
                s.FullName,
                s.Gender,
                GradeNameAr = s.Section.Grade.NameAr,
                SectionNameAr = s.Section.NameAr,
                GuardianNameAr = (string?)s.Guardian.FullName,
                GuardianPhone = s.Guardian.Phone,
                s.Status,
                s.EnrollmentDate
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var mapped = rows
            .Select(r => new StudentReportRow(
                r.Id,
                r.StudentNumber,
                r.FullName,
                GenderToAr(r.Gender),
                r.GradeNameAr,
                r.SectionNameAr,
                r.GuardianNameAr,
                r.GuardianPhone,
                StudentStatusToAr(r.Status),
                r.EnrollmentDate))
            .ToList();

        string? gradeName = null;
        string? sectionName = null;
        if (query.GradeId.HasValue)
        {
            gradeName = await ctx.Grades
                .AsNoTracking()
                .Where(g => g.Id == query.GradeId.Value)
                .Select(g => g.NameAr)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
        if (query.SectionId.HasValue)
        {
            sectionName = await ctx.Sections
                .AsNoTracking()
                .Where(s => s.Id == query.SectionId.Value)
                .Select(s => s.NameAr)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        var statusLabel = query.Status switch
        {
            StudentStatus.Active => "نشط",
            StudentStatus.Archived => "مؤرشف",
            StudentStatus.Graduated => "متخرّج",
            _ => "جميع الحالات"
        };

        return new StudentsReportResult(
            header.SchoolName,
            header.Address,
            header.Phone,
            header.AcademicYearAr,
            gradeName,
            sectionName,
            statusLabel,
            DateTime.Now,
            mapped);
    }

    // -------------------- Attendance Report --------------------

    public async Task<AttendanceReportResult> GetAttendanceReportAsync(AttendanceReportQuery query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var header = await GetHeaderTupleAsync(ctx, ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var fromDate = query.DateFrom.Date;
        var toDateExclusive = query.DateTo.Date.AddDays(1);
        // If user passed reversed dates, swap defensively.
        if (toDateExclusive <= fromDate)
        {
            var tmp = fromDate;
            fromDate = query.DateTo.Date;
            toDateExclusive = tmp.AddDays(1);
        }

        string? gradeName = null;
        string? sectionName = null;
        if (query.GradeId.HasValue)
        {
            gradeName = await ctx.Grades.AsNoTracking()
                .Where(g => g.Id == query.GradeId.Value)
                .Select(g => g.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
        if (query.SectionId.HasValue)
        {
            sectionName = await ctx.Sections.AsNoTracking()
                .Where(s => s.Id == query.SectionId.Value)
                .Select(s => s.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        if (!yearId.HasValue)
        {
            return new AttendanceReportResult(
                header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
                query.DateFrom.Date, query.DateTo.Date, gradeName, sectionName,
                DateTime.Now,
                Array.Empty<AttendanceReportRow>(),
                new AttendanceReportTotals(0, 0, 0, 0, 0, 0, 0.0));
        }

        // Historical-truth: do NOT filter by current Status. A student archived after
        // the period must still appear with their attendance for the queried dates.
        var students = ctx.Students.AsNoTracking()
            .Where(s => s.Section.AcademicYearId == yearId.Value);

        if (query.GradeId.HasValue)
            students = students.Where(s => s.Section.GradeId == query.GradeId.Value);
        if (query.SectionId.HasValue)
            students = students.Where(s => s.SectionId == query.SectionId.Value);

        // Per-student aggregate via correlated subqueries (single SQL round-trip).
        var raw = await students
            .OrderBy(s => s.Section.Grade.SortOrder)
            .ThenBy(s => s.Section.NameAr)
            .ThenBy(s => s.StudentNumber)
            .Select(s => new
            {
                StudentId = s.Id,
                s.StudentNumber,
                s.FullName,
                GradeNameAr = s.Section.Grade.NameAr,
                SectionNameAr = s.Section.NameAr,
                Present = s.AttendanceRecords.Count(a => a.Date >= fromDate && a.Date < toDateExclusive && a.Status == AttendanceStatus.Present),
                Absent = s.AttendanceRecords.Count(a => a.Date >= fromDate && a.Date < toDateExclusive && a.Status == AttendanceStatus.Absent),
                Late = s.AttendanceRecords.Count(a => a.Date >= fromDate && a.Date < toDateExclusive && a.Status == AttendanceStatus.Late),
                Excused = s.AttendanceRecords.Count(a => a.Date >= fromDate && a.Date < toDateExclusive && a.Status == AttendanceStatus.Excused)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rows = new List<AttendanceReportRow>(raw.Count);
        int totalPresent = 0, totalAbsent = 0, totalLate = 0, totalExcused = 0, totalRecorded = 0;
        double percentSum = 0.0;
        int percentCount = 0;

        foreach (var r in raw)
        {
            var recorded = r.Present + r.Absent + r.Late + r.Excused;
            double pct = recorded > 0
                ? (r.Present + r.Late * 0.5) / recorded * 100.0
                : 0.0;

            rows.Add(new AttendanceReportRow(
                r.StudentId,
                r.StudentNumber,
                r.FullName,
                r.GradeNameAr,
                r.SectionNameAr,
                r.Present,
                r.Absent,
                r.Late,
                r.Excused,
                recorded,
                pct));

            totalPresent += r.Present;
            totalAbsent += r.Absent;
            totalLate += r.Late;
            totalExcused += r.Excused;
            totalRecorded += recorded;
            if (recorded > 0)
            {
                percentSum += pct;
                percentCount++;
            }
        }

        var avgPct = percentCount > 0 ? percentSum / percentCount : 0.0;
        var totals = new AttendanceReportTotals(
            rows.Count,
            totalRecorded,
            totalPresent,
            totalAbsent,
            totalLate,
            totalExcused,
            avgPct);

        return new AttendanceReportResult(
            header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
            query.DateFrom.Date, query.DateTo.Date, gradeName, sectionName,
            DateTime.Now, rows, totals);
    }

    // -------------------- Marks Report --------------------

    public async Task<MarksReportResult> GetMarksReportAsync(MarksReportQuery query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var header = await GetHeaderTupleAsync(ctx, ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var gradeName = await ctx.Grades.AsNoTracking()
            .Where(g => g.Id == query.GradeId)
            .Select(g => g.NameAr)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? string.Empty;
        var sectionName = await ctx.Sections.AsNoTracking()
            .Where(s => s.Id == query.SectionId)
            .Select(s => s.NameAr)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? string.Empty;

        var examName = "كل الامتحانات";
        if (query.ExamId.HasValue)
        {
            examName = await ctx.Exams.AsNoTracking()
                .Where(e => e.Id == query.ExamId.Value)
                .Select(e => e.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? string.Empty;
        }

        // Subjects (columns)
        var subjects = await ctx.Subjects.AsNoTracking()
            .Where(s => s.GradeId == query.GradeId)
            .OrderBy(s => s.NameAr)
            .Select(s => new { s.Id, s.NameAr, s.MaxMark, s.PassMark })
            .ToListAsync(ct).ConfigureAwait(false);

        var columns = subjects
            .Select(s => new MarksReportColumn(s.Id, s.NameAr, (int)s.MaxMark, (int)s.PassMark))
            .ToList();

        // Empty fallback helper
        MarksReportResult Empty(IReadOnlyList<MarksReportColumn> cols) => new(
            header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
            gradeName, sectionName, examName, !query.ExamId.HasValue,
            DateTime.Now, cols, Array.Empty<MarksReportRow>(),
            new MarksReportTotals(0, 0, 0, 0, 0.0, 0.0, 0.0));

        if (!yearId.HasValue || subjects.Count == 0)
            return Empty(columns);

        // Roster
        var students = await ctx.Students.AsNoTracking()
            .Where(s => s.SectionId == query.SectionId
                     && s.Section.AcademicYearId == yearId.Value
                     && s.Status == StudentStatus.Active)
            .Select(s => new { s.Id, s.StudentNumber, s.FullName })
            .ToListAsync(ct).ConfigureAwait(false);

        if (students.Count == 0)
            return Empty(columns);

        var studentIds = students.Select(s => s.Id).ToList();
        var subjectIds = subjects.Select(s => s.Id).ToList();

        var rows = new List<MarksReportRow>(students.Count);

        if (query.ExamId.HasValue)
        {
            // Single batched marks query
            var examId = query.ExamId.Value;
            var marks = await ctx.Marks.AsNoTracking()
                .Where(m => m.ExamId == examId
                         && subjectIds.Contains(m.SubjectId)
                         && studentIds.Contains(m.StudentId))
                .Select(m => new { m.StudentId, m.SubjectId, m.Value })
                .ToListAsync(ct).ConfigureAwait(false);

            var marksLookup = marks
                .GroupBy(m => m.StudentId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.SubjectId, x => x.Value));

            foreach (var st in students)
            {
                var cells = new List<MarksReportCell>(subjects.Count);
                decimal total = 0m;
                decimal maxTotal = 0m;
                int nonNullCount = 0;
                int failedCount = 0;

                marksLookup.TryGetValue(st.Id, out var perSubject);

                foreach (var sub in subjects)
                {
                    decimal? mark = null;
                    if (perSubject is not null && perSubject.TryGetValue(sub.Id, out var v))
                        mark = v;

                    bool isPass = mark.HasValue && mark.Value >= sub.PassMark;
                    cells.Add(new MarksReportCell(sub.Id, mark, isPass, IsAbsent: false));

                    if (mark.HasValue)
                    {
                        total += mark.Value;
                        maxTotal += sub.MaxMark;
                        nonNullCount++;
                        if (!isPass) failedCount++;
                    }
                }

                double pct = maxTotal > 0m ? (double)(total / maxTotal) * 100.0 : 0.0;

                bool isPending;
                bool isPassed;
                string statusAr;
                string gradeLabel;

                if (nonNullCount == 0)
                {
                    isPending = true;
                    isPassed = false;
                    statusAr = "غير مكتمل";
                    gradeLabel = GradeLabel(ResultGrade.Pending);
                }
                else if (failedCount > 0)
                {
                    isPending = false;
                    isPassed = false;
                    statusAr = "راسب";
                    gradeLabel = GradeLabel(ResultGrade.Failed);
                }
                else if (nonNullCount < subjects.Count)
                {
                    isPending = true;
                    isPassed = false;
                    statusAr = "غير مكتمل";
                    gradeLabel = GradeLabel(ResultGrade.Pending);
                }
                else
                {
                    isPending = false;
                    isPassed = true;
                    statusAr = "ناجح";
                    gradeLabel = GradeLabel(ComputeGradeFromPercentage((decimal)pct));
                }

                rows.Add(new MarksReportRow(
                    st.Id, st.StudentNumber, st.FullName,
                    cells, total, maxTotal, pct, gradeLabel, statusAr, isPassed, isPending));
            }
        }
        else
        {
            // Aggregate across all exams via IResultsCalculator
            var exams = await ctx.Exams.AsNoTracking()
                .Where(e => e.AcademicYearId == yearId.Value)
                .Select(e => new { e.Id, e.Weight })
                .ToListAsync(ct).ConfigureAwait(false);

            var allMarks = await ctx.Marks.AsNoTracking()
                .Where(m => subjectIds.Contains(m.SubjectId)
                         && studentIds.Contains(m.StudentId)
                         && exams.Select(e => e.Id).Contains(m.ExamId))
                .Select(m => new { m.StudentId, m.SubjectId, m.ExamId, m.Value })
                .ToListAsync(ct).ConfigureAwait(false);

            var marksByStudent = allMarks
                .GroupBy(m => m.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var st in students)
            {
                marksByStudent.TryGetValue(st.Id, out var studentMarks);
                studentMarks ??= new();

                var subjectInputs = subjects.Select(sub =>
                {
                    var examInputs = exams.Select(e =>
                    {
                        decimal? value = studentMarks
                            .Where(m => m.SubjectId == sub.Id && m.ExamId == e.Id)
                            .Select(m => (decimal?)m.Value)
                            .FirstOrDefault();
                        return new ExamMarkInput(e.Id, e.Weight, value);
                    }).ToList();

                    return new SubjectMarksInput(sub.Id, sub.NameAr, sub.MaxMark, sub.PassMark, examInputs);
                }).ToList();

                var input = new StudentMarksInput(st.Id, st.StudentNumber, st.FullName, subjectInputs);
                var summary = _resultsCalculator.Compute(input);

                // Treat "no mark entered" (Score == null) as "missing", not absent. IsAbsent is
                // reserved for cells where the student was explicitly marked absent at exam time.
                var cells = summary.Subjects.Select(sr => new MarksReportCell(
                    sr.SubjectId,
                    sr.Score,
                    sr.IsPassed,
                    sr.IsAbsent && sr.Score is not null)).ToList();

                bool isPending = summary.Grade == ResultGrade.Pending;
                double pct = (double)summary.Percentage;
                string statusAr = isPending ? "غير مكتمل" : (summary.IsPassed ? "ناجح" : "راسب");
                string gradeLabel = GradeLabel(summary.Grade);

                rows.Add(new MarksReportRow(
                    st.Id, st.StudentNumber, st.FullName,
                    cells, summary.Total, summary.ExaminedMax, pct,
                    gradeLabel, statusAr, summary.IsPassed, isPending));
            }
        }

        // Sort by Total desc, then FullName
        rows = rows.OrderByDescending(r => r.Total).ThenBy(r => r.FullName).ToList();

        // Totals
        int passedCount = rows.Count(r => r.IsPassed);
        int pendingCount = rows.Count(r => r.IsPending);
        int failedCount2 = rows.Count - passedCount - pendingCount;
        var nonPending = rows.Where(r => !r.IsPending).Select(r => r.Percentage).ToList();
        double avgPct = nonPending.Count > 0 ? nonPending.Average() : 0.0;
        double highest = nonPending.Count > 0 ? nonPending.Max() : 0.0;
        double lowest = nonPending.Count > 0 ? nonPending.Min() : 0.0;

        var totals = new MarksReportTotals(
            rows.Count, passedCount, failedCount2, pendingCount, avgPct, highest, lowest);

        return new MarksReportResult(
            header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
            gradeName, sectionName, examName, !query.ExamId.HasValue,
            DateTime.Now, columns, rows, totals);
    }

    // -------------------- Fees Report --------------------

    public async Task<FeesReportResult> GetFeesReportAsync(FeesReportQuery query, CancellationToken ct = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var header = await GetHeaderTupleAsync(ctx, ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        string? gradeName = null;
        string? sectionName = null;
        if (query.GradeId.HasValue)
        {
            gradeName = await ctx.Grades.AsNoTracking()
                .Where(g => g.Id == query.GradeId.Value)
                .Select(g => g.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
        if (query.SectionId.HasValue)
        {
            sectionName = await ctx.Sections.AsNoTracking()
                .Where(s => s.Id == query.SectionId.Value)
                .Select(s => s.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        var statusLabel = query.Status switch
        {
            FeesReportStatusFilter.FullyPaid => "مسدّد بالكامل",
            FeesReportStatusFilter.PartiallyPaid => "دفع جزئي",
            FeesReportStatusFilter.Unpaid => "غير مدفوع",
            FeesReportStatusFilter.HasOverdue => "عليه متأخرات",
            _ => "كل الحالات"
        };

        if (!yearId.HasValue)
        {
            return new FeesReportResult(
                header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
                gradeName, sectionName, query.Status, statusLabel,
                DateTime.Now, Array.Empty<FeesReportRow>(),
                new FeesReportTotals(0, 0m, 0m, 0m, 0));
        }

        var today = DateTime.Today;

        var studentsQ = ctx.Students.AsNoTracking()
            .Where(s => s.Status == StudentStatus.Active
                     && s.Section.AcademicYearId == yearId.Value);

        if (query.GradeId.HasValue)
            studentsQ = studentsQ.Where(s => s.Section.GradeId == query.GradeId.Value);
        if (query.SectionId.HasValue)
            studentsQ = studentsQ.Where(s => s.SectionId == query.SectionId.Value);

        // Project with left-join to StudentFee for the current year.
        var raw = await studentsQ
            .OrderBy(s => s.Section.Grade.SortOrder)
            .ThenBy(s => s.Section.NameAr)
            .ThenBy(s => s.FullName)
            .Select(s => new
            {
                StudentId = s.Id,
                s.StudentNumber,
                s.FullName,
                GradeNameAr = s.Section.Grade.NameAr,
                SectionNameAr = s.Section.NameAr,
                Fee = s.StudentFees
                    .Where(f => f.FeePlan.AcademicYearId == yearId.Value)
                    .Select(f => new
                    {
                        f.Id,
                        FeePlanNameAr = f.FeePlan.NameAr,
                        f.TotalAmount,
                        f.PaidAmount,
                        OverdueCount = f.Installments.Count(i =>
                            i.Status == InstallmentStatus.Overdue
                            || (i.Status == InstallmentStatus.Due && i.DueDate < today))
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(ct).ConfigureAwait(false);

        var allRows = raw.Select(r =>
        {
            if (r.Fee is null)
            {
                return new FeesReportRow(
                    r.StudentId, r.StudentNumber, r.FullName,
                    r.GradeNameAr, r.SectionNameAr,
                    null, 0m, 0m, 0m, 0,
                    "بدون خطة");
            }

            var remaining = r.Fee.TotalAmount - r.Fee.PaidAmount;
            string statusAr;
            if (r.Fee.OverdueCount > 0)
                statusAr = "عليه أقساط متأخرة";
            else if (r.Fee.TotalAmount > 0m && remaining <= 0m)
                statusAr = "مدفوع";
            else if (r.Fee.PaidAmount > 0m)
                statusAr = "جزئي";
            else
                statusAr = "غير مدفوع";

            return new FeesReportRow(
                r.StudentId, r.StudentNumber, r.FullName,
                r.GradeNameAr, r.SectionNameAr,
                r.Fee.FeePlanNameAr,
                r.Fee.TotalAmount,
                r.Fee.PaidAmount,
                remaining,
                r.Fee.OverdueCount,
                statusAr);
        }).ToList();

        // Apply status filter in memory (small set, simple logic).
        var filtered = query.Status switch
        {
            FeesReportStatusFilter.FullyPaid => allRows.Where(x => x.TotalAmount > 0m && x.RemainingAmount <= 0m).ToList(),
            FeesReportStatusFilter.PartiallyPaid => allRows.Where(x => x.PaidAmount > 0m && x.RemainingAmount > 0m).ToList(),
            FeesReportStatusFilter.Unpaid => allRows.Where(x => (x.FeePlanNameAr is null || x.PaidAmount == 0m) && x.OverdueCount == 0).ToList(),
            FeesReportStatusFilter.HasOverdue => allRows.Where(x => x.OverdueCount > 0).ToList(),
            _ => allRows
        };

        var totals = new FeesReportTotals(
            filtered.Count,
            filtered.Sum(x => x.TotalAmount),
            filtered.Sum(x => x.PaidAmount),
            filtered.Sum(x => x.RemainingAmount),
            filtered.Sum(x => x.OverdueCount));

        return new FeesReportResult(
            header.SchoolName, header.Address, header.Phone, header.AcademicYearAr,
            gradeName, sectionName, query.Status, statusLabel,
            DateTime.Now, filtered, totals);
    }

    // -------------------- Helpers --------------------

    private static async Task<int?> GetCurrentYearIdAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        var fromSettings = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => s.CurrentAcademicYearId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (fromSettings.HasValue) return fromSettings.Value;

        return await ctx.AcademicYears
            .AsNoTracking()
            .Where(y => y.IsActive)
            .OrderByDescending(y => y.StartDate)
            .Select(y => (int?)y.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private static async Task<(string SchoolName, string? Address, string? Phone, string AcademicYearAr)>
        GetHeaderTupleAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        var settings = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => new
            {
                s.NameAr,
                s.Address,
                s.Phone,
                s.CurrentAcademicYearId
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var schoolName = settings?.NameAr ?? "نَسَق لإدارة المدارس";
        var address = settings?.Address;
        var phone = settings?.Phone;
        var yearId = settings?.CurrentAcademicYearId;

        string yearAr = string.Empty;
        if (yearId.HasValue)
        {
            yearAr = await ctx.AcademicYears.AsNoTracking()
                .Where(y => y.Id == yearId.Value)
                .Select(y => y.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? string.Empty;
        }
        else
        {
            yearAr = await ctx.AcademicYears.AsNoTracking()
                .Where(y => y.IsActive)
                .OrderByDescending(y => y.StartDate)
                .Select(y => y.NameAr)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? string.Empty;
        }

        return (schoolName, address, phone, yearAr);
    }

    private static string GenderToAr(Gender g) => g switch
    {
        Gender.Male => "ذكر",
        Gender.Female => "أنثى",
        _ => string.Empty
    };

    private static string StudentStatusToAr(StudentStatus s) => s switch
    {
        StudentStatus.Active => "نشط",
        StudentStatus.Archived => "مؤرشف",
        StudentStatus.Graduated => "متخرّج",
        _ => string.Empty
    };

    private static string GradeLabel(ResultGrade g) => g switch
    {
        ResultGrade.Excellent => "ممتاز",
        ResultGrade.VeryGood => "جيد جداً",
        ResultGrade.Good => "جيد",
        ResultGrade.Acceptable => "مقبول",
        ResultGrade.Pending => "غير مكتمل",
        ResultGrade.Failed => "راسب",
        _ => string.Empty
    };

    // Mirrors thresholds in ResultsCalculator (kept private there).
    private static ResultGrade ComputeGradeFromPercentage(decimal percentage)
    {
        if (percentage >= 90m) return ResultGrade.Excellent;
        if (percentage >= 80m) return ResultGrade.VeryGood;
        if (percentage >= 70m) return ResultGrade.Good;
        return ResultGrade.Acceptable;
    }
}
