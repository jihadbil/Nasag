using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Repositories;

public sealed class MarksRepository : IMarksRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public MarksRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<MarksLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .Where(g => g.Sections.Any(s => yearId == null || s.AcademicYearId == yearId))
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new MarksGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections
            .AsNoTracking()
            .Where(s => yearId == null || s.AcademicYearId == yearId)
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new MarksSectionOption(
                s.Id,
                s.NameAr,
                s.GradeId,
                s.Grade.NameAr,
                s.Students.Count(st => st.Status == StudentStatus.Active)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var subjects = await ctx.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new MarksSubjectOption(s.Id, s.NameAr, s.GradeId, s.MaxMark, s.PassMark))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var exams = await ctx.Exams
            .AsNoTracking()
            .Where(e => yearId == null || e.AcademicYearId == yearId)
            .OrderBy(e => e.NameAr)
            .Select(e => new MarksExamOption(e.Id, e.NameAr, e.Weight))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new MarksLookups(grades, sections, subjects, exams);
    }

    public async Task<MarksSheet> GetSheetAsync(int sectionId, int subjectId, int examId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var section = await ctx.Sections
            .AsNoTracking()
            .Where(s => s.Id == sectionId)
            .Select(s => new { s.Id, s.GradeId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (section is null)
            throw new InvalidOperationException("الشعبة المحددة غير موجودة.");

        var subject = await ctx.Subjects
            .AsNoTracking()
            .Where(s => s.Id == subjectId)
            .Select(s => new { s.Id, s.GradeId, s.MaxMark, s.PassMark })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (subject is null)
            throw new InvalidOperationException("المادة المحددة غير موجودة.");

        if (subject.GradeId != section.GradeId)
            throw new InvalidOperationException("المادة المحددة لا تنتمي إلى صف الشعبة.");

        var examExists = await ctx.Exams
            .AsNoTracking()
            .AnyAsync(e => e.Id == examId, ct)
            .ConfigureAwait(false);
        if (!examExists)
            throw new InvalidOperationException("الامتحان المحدد غير موجود.");

        var students = await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.FullName)
            .Select(s => new { s.Id, s.StudentNumber, s.FullName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = students.Select(s => s.Id).ToArray();
        var existing = ids.Length == 0
            ? new List<Mark>()
            : await ctx.Marks
                .AsNoTracking()
                .Where(m => ids.Contains(m.StudentId) && m.SubjectId == subjectId && m.ExamId == examId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var byStudent = existing.ToDictionary(m => m.StudentId);

        var rows = students
            .Select(s =>
            {
                byStudent.TryGetValue(s.Id, out var mark);
                return new MarksStudentRow(
                    s.Id,
                    s.StudentNumber,
                    s.FullName,
                    mark?.Value,
                    mark?.Notes,
                    mark?.Id);
            })
            .ToList();

        return new MarksSheet(sectionId, subjectId, examId, subject.MaxMark, subject.PassMark, rows);
    }

    public async Task SaveSheetAsync(
        int sectionId,
        int subjectId,
        int examId,
        IReadOnlyList<MarkSaveRow> rows,
        CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var section = await ctx.Sections
                .AsNoTracking()
                .Where(s => s.Id == sectionId)
                .Select(s => new { s.Id, s.GradeId })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (section is null)
                throw new InvalidOperationException("الشعبة المحددة غير موجودة.");

            var subject = await ctx.Subjects
                .AsNoTracking()
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.Id, s.GradeId, s.MaxMark, s.PassMark, s.NameAr })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (subject is null)
                throw new InvalidOperationException("المادة المحددة غير موجودة.");

            if (subject.GradeId != section.GradeId)
                throw new InvalidOperationException("المادة المحددة لا تنتمي إلى صف الشعبة.");

            var examExists = await ctx.Exams
                .AsNoTracking()
                .AnyAsync(e => e.Id == examId, ct)
                .ConfigureAwait(false);
            if (!examExists)
                throw new InvalidOperationException("الامتحان المحدد غير موجود.");

            var activeStudents = await ctx.Students
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
                .Select(s => new { s.Id, s.FullName })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var activeSet = activeStudents.ToDictionary(s => s.Id, s => s.FullName);
            var activeIds = new HashSet<int>(activeSet.Keys);

            var distinctIds = rows.Select(r => r.StudentId).Distinct().ToList();
            var invalid = distinctIds.Where(id => !activeIds.Contains(id)).ToList();
            if (invalid.Count > 0)
                throw new InvalidOperationException("تحتوي القائمة على طلاب غير نشطين أو خارج الشعبة المحددة.");

            var submitted = rows
                .GroupBy(r => r.StudentId)
                .Select(g => g.Last())
                .ToList();

            var maxMark = subject.MaxMark;

            foreach (var row in submitted)
            {
                if (row.Value.HasValue && (row.Value.Value < 0m || row.Value.Value > maxMark))
                {
                    var name = activeSet[row.StudentId];
                    var maxText = maxMark.ToString("0.##", CultureInfo.InvariantCulture);
                    throw new InvalidOperationException(
                        $"الدرجة يجب أن تكون بين 0 و {maxText} للطالب {name}.");
                }
            }

            var ids = submitted.Select(r => r.StudentId).ToArray();
            var existing = ids.Length == 0
                ? new List<Mark>()
                : await ctx.Marks
                    .Where(m => ids.Contains(m.StudentId) && m.SubjectId == subjectId && m.ExamId == examId)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var byStudent = existing.ToDictionary(m => m.StudentId);
            var now = DateTime.UtcNow;

            foreach (var row in submitted)
            {
                var notes = NormalizeNotes(row.Notes);
                byStudent.TryGetValue(row.StudentId, out var mark);

                if (!row.Value.HasValue)
                {
                    if (mark is not null)
                        ctx.Marks.Remove(mark);
                    continue;
                }

                if (mark is not null)
                {
                    mark.Value = row.Value.Value;
                    mark.Notes = notes;
                    mark.UpdatedAt = now;
                }
                else
                {
                    ctx.Marks.Add(new Mark
                    {
                        StudentId = row.StudentId,
                        SubjectId = subjectId,
                        ExamId = examId,
                        Value = row.Value.Value,
                        Notes = notes,
                        UpdatedAt = now
                    });
                }
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

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

    private static string? NormalizeNotes(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length <= 300 ? text : text[..300];
    }
}
