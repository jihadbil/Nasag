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

public sealed class ResultsRepository : IResultsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public ResultsRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<ResultsLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new ResultsGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections
            .AsNoTracking()
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new ResultsSectionOption(
                s.Id,
                s.NameAr,
                s.GradeId,
                s.Grade.NameAr,
                s.AcademicYearId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var years = await ctx.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => new ResultsYearOption(y.Id, y.NameAr, y.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ResultsLookups(grades, sections, years);
    }

    public async Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

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

    public async Task<IReadOnlyList<StudentMarksInput>> GetStudentInputsAsync(
        int sectionId,
        int academicYearId,
        CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var section = await ctx.Sections
            .AsNoTracking()
            .Where(s => s.Id == sectionId)
            .Select(s => new { s.Id, s.GradeId, s.AcademicYearId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (section is null)
            throw new InvalidOperationException("الشعبة المحددة غير موجودة.");

        if (section.AcademicYearId != academicYearId)
            throw new InvalidOperationException("الشعبة المحددة لا تنتمي إلى السنة الدراسية المختارة.");

        var gradeId = section.GradeId;

        var students = await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.FullName)
            .Select(s => new { s.Id, s.StudentNumber, s.FullName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (students.Count == 0)
            return Array.Empty<StudentMarksInput>();

        var subjects = await ctx.Subjects
            .AsNoTracking()
            .Where(s => s.GradeId == gradeId)
            .OrderBy(s => s.NameAr)
            .Select(s => new { s.Id, s.NameAr, s.MaxMark, s.PassMark })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var exams = await ctx.Exams
            .AsNoTracking()
            .Where(e => e.AcademicYearId == academicYearId)
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, e.NameAr, e.Weight })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var studentIds = students.Select(s => s.Id).ToArray();
        var examIds = exams.Select(e => e.Id).ToArray();

        List<Mark> marks;
        if (subjects.Count == 0 || exams.Count == 0)
        {
            marks = new List<Mark>();
        }
        else
        {
            marks = await ctx.Marks
                .AsNoTracking()
                .Where(m =>
                    studentIds.Contains(m.StudentId)
                    && m.Subject.GradeId == gradeId
                    && examIds.Contains(m.ExamId))
                .Select(m => new Mark
                {
                    StudentId = m.StudentId,
                    SubjectId = m.SubjectId,
                    ExamId = m.ExamId,
                    Value = m.Value
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        // (StudentId, SubjectId, ExamId) -> Value
        var marksLookup = marks
            .GroupBy(m => (m.StudentId, m.SubjectId, m.ExamId))
            .ToDictionary(g => g.Key, g => g.First().Value);

        var result = new List<StudentMarksInput>(students.Count);
        foreach (var student in students)
        {
            var subjectInputs = new List<SubjectMarksInput>(subjects.Count);
            foreach (var subject in subjects)
            {
                var examInputs = new List<ExamMarkInput>(exams.Count);
                foreach (var exam in exams)
                {
                    decimal? value = marksLookup.TryGetValue((student.Id, subject.Id, exam.Id), out var v)
                        ? v
                        : null;
                    examInputs.Add(new ExamMarkInput(exam.Id, exam.Weight, value));
                }

                subjectInputs.Add(new SubjectMarksInput(
                    subject.Id,
                    subject.NameAr,
                    subject.MaxMark,
                    subject.PassMark,
                    examInputs));
            }

            result.Add(new StudentMarksInput(
                student.Id,
                student.StudentNumber,
                student.FullName,
                subjectInputs));
        }

        return result;
    }
}
