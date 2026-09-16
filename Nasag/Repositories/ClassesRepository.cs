using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Repositories;

public sealed class ClassesRepository : IClassesRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public ClassesRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<GradeRow>> GetGradesAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);

        return await ctx.Grades
            .AsNoTracking()
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .Select(g => new GradeRow(
                g.Id,
                g.NameAr,
                g.Level,
                g.SortOrder,
                g.Sections.Count(s => yearId == null || s.AcademicYearId == yearId),
                g.Sections
                    .Where(s => yearId == null || s.AcademicYearId == yearId)
                    .SelectMany(s => s.Students)
                    .Count(st => st.Status == StudentStatus.Active)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SectionRow>> GetSectionsForGradeAsync(int gradeId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);

        return await ctx.Sections
            .AsNoTracking()
            .Where(s => s.GradeId == gradeId && (yearId == null || s.AcademicYearId == yearId))
            .OrderBy(s => s.NameAr)
            .Select(s => new SectionRow(
                s.Id,
                s.GradeId,
                s.Grade.NameAr,
                s.NameAr,
                s.Capacity,
                s.Students.Count(st => st.Status == StudentStatus.Active)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SectionStudentRow>> GetStudentsForSectionAsync(int sectionId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId)
            .OrderBy(s => s.FullName)
            .Select(s => new SectionStudentRow(
                s.Id,
                s.StudentNumber,
                s.FullName,
                s.Gender,
                s.Status,
                s.Phone))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ClassesStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);

        var grades = await ctx.Grades.CountAsync(ct).ConfigureAwait(false);
        var sections = await ctx.Sections
            .Where(s => yearId == null || s.AcademicYearId == yearId)
            .CountAsync(ct).ConfigureAwait(false);
        var students = await ctx.Students
            .Where(s => s.Status == StudentStatus.Active &&
                        (yearId == null || s.Section.AcademicYearId == yearId))
            .CountAsync(ct).ConfigureAwait(false);

        return new ClassesStats(grades, sections, students);
    }

    public async Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);
    }

    private static async Task<int?> GetCurrentYearIdInternalAsync(NasaqDbContext ctx, CancellationToken ct)
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

    public async Task<int> CreateGradeAsync(GradeSaveModel model, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var grade = new Grade
        {
            NameAr = model.NameAr.Trim(),
            Level = model.Level,
            SortOrder = model.SortOrder,
        };
        ctx.Grades.Add(grade);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return grade.Id;
    }

    public async Task UpdateGradeAsync(GradeSaveModel model, CancellationToken ct = default)
    {
        if (model.Id is null) throw new InvalidOperationException("Grade Id is required for update.");
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grade = await ctx.Grades.FirstOrDefaultAsync(g => g.Id == model.Id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("الصف غير موجود.");
        grade.NameAr = model.NameAr.Trim();
        grade.Level = model.Level;
        grade.SortOrder = model.SortOrder;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<GradeDependencyCounts> GetGradeDependencyCountsAsync(int gradeId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sectionCount = await ctx.Sections.CountAsync(s => s.GradeId == gradeId, ct).ConfigureAwait(false);
        var studentCount = await ctx.Students.CountAsync(s => s.Section.GradeId == gradeId, ct).ConfigureAwait(false);
        var subjectCount = await ctx.Subjects.CountAsync(s => s.GradeId == gradeId, ct).ConfigureAwait(false);
        return new GradeDependencyCounts(sectionCount, studentCount, subjectCount);
    }

    public async Task<SectionDependencyCounts> GetSectionDependencyCountsAsync(int sectionId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var studentCount = await ctx.Students.CountAsync(s => s.SectionId == sectionId, ct).ConfigureAwait(false);
        return new SectionDependencyCounts(studentCount);
    }

    public async Task DeleteGradeAsync(int gradeId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var sectionIds = await ctx.Sections
                .Where(s => s.GradeId == gradeId)
                .Select(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var sectionId in sectionIds)
                await DeleteSectionWithStudentsAsync(ctx, sectionId, ct).ConfigureAwait(false);

            // Subjects of the grade — cascade with marks first.
            var subjectIds = await ctx.Subjects
                .Where(s => s.GradeId == gradeId)
                .Select(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (subjectIds.Count > 0)
            {
                await ctx.Marks
                    .Where(m => subjectIds.Contains(m.SubjectId))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await ctx.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            }

            // FeePlans tied to the grade — cascade student-fees -> installments -> payments first.
            var feePlanIds = await ctx.FeePlans
                .Where(p => p.GradeId == gradeId)
                .Select(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (feePlanIds.Count > 0)
            {
                await ctx.Payments
                    .Where(p => feePlanIds.Contains(p.StudentFee.FeePlanId))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await ctx.Installments
                    .Where(i => feePlanIds.Contains(i.StudentFee.FeePlanId))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await ctx.StudentFees
                    .Where(f => feePlanIds.Contains(f.FeePlanId))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await ctx.FeePlans
                    .Where(p => feePlanIds.Contains(p.Id))
                    .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            }

            var deleted = await ctx.Grades
                .Where(g => g.Id == gradeId)
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            if (deleted == 0)
                throw new InvalidOperationException("الصف غير موجود.");

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<int> CreateSectionAsync(SectionSaveModel model, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var yearId = await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("لا توجد سنة دراسية نشطة.");

        var name = model.NameAr.Trim();
        var clash = await ctx.Sections
            .AnyAsync(s => s.GradeId == model.GradeId && s.AcademicYearId == yearId && s.NameAr == name, ct)
            .ConfigureAwait(false);
        if (clash) throw new InvalidOperationException("توجد شعبة بهذا الاسم في هذا الصف للسنة الحالية.");

        var section = new Section
        {
            NameAr = name,
            Capacity = Math.Max(1, model.Capacity),
            GradeId = model.GradeId,
            AcademicYearId = yearId,
        };
        ctx.Sections.Add(section);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return section.Id;
    }

    public async Task UpdateSectionAsync(SectionSaveModel model, CancellationToken ct = default)
    {
        if (model.Id is null) throw new InvalidOperationException("Section Id is required for update.");
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var section = await ctx.Sections.FirstOrDefaultAsync(s => s.Id == model.Id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("الشعبة غير موجودة.");

        var name = model.NameAr.Trim();
        var clash = await ctx.Sections
            .AnyAsync(s => s.Id != model.Id &&
                           s.GradeId == section.GradeId &&
                           s.AcademicYearId == section.AcademicYearId &&
                           s.NameAr == name, ct)
            .ConfigureAwait(false);
        if (clash) throw new InvalidOperationException("توجد شعبة أخرى بهذا الاسم في الصف نفسه.");

        section.NameAr = name;
        section.Capacity = Math.Max(1, model.Capacity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteSectionAsync(int sectionId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await DeleteSectionWithStudentsAsync(ctx, sectionId, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Section delete helper — wipes payments → installments → fees → attendance →
    /// marks → students (and orphan guardians) → section in one nested unit of work.
    /// Mirrors the cascade order used by StudentsRepository.DeleteAsync.
    /// </summary>
    private static async Task DeleteSectionWithStudentsAsync(NasaqDbContext ctx, int sectionId, CancellationToken ct)
    {
        var students = await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId)
            .Select(s => new { s.Id, s.GuardianId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (students.Count > 0)
        {
            var ids = students.Select(s => s.Id).ToArray();
            var guardianIds = students.Select(s => s.GuardianId).Distinct().ToArray();

            await ctx.Payments
                .Where(p => ids.Contains(p.StudentFee.StudentId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.Installments
                .Where(i => ids.Contains(i.StudentFee.StudentId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.StudentFees
                .Where(f => ids.Contains(f.StudentId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.AttendanceRecords
                .Where(a => ids.Contains(a.StudentId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.Marks
                .Where(m => ids.Contains(m.StudentId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.Students
                .Where(s => ids.Contains(s.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);

            // Drop guardians who no longer reference any student.
            await ctx.Guardians
                .Where(g => guardianIds.Contains(g.Id) && !ctx.Students.Any(s => s.GuardianId == g.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        }

        var sectionDeleted = await ctx.Sections
            .Where(s => s.Id == sectionId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        if (sectionDeleted == 0)
            throw new InvalidOperationException("الشعبة غير موجودة.");
    }

    public async Task MoveStudentAsync(int studentId, int targetSectionId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var student = await ctx.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("الطالب غير موجود.");

        if (student.SectionId == targetSectionId) return;

        var target = await ctx.Sections
            .AsNoTracking()
            .Where(s => s.Id == targetSectionId)
            .Select(s => new { s.Id, s.Capacity, Count = s.Students.Count() })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("الشعبة الهدف غير موجودة.");

        if (target.Count >= target.Capacity)
            throw new InvalidOperationException(
                $"الشعبة الهدف ممتلئة ({target.Count}/{target.Capacity}). ارفع السعة أو اختر شعبة أخرى.");

        student.SectionId = targetSectionId;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MoveTargetSection>> GetMoveTargetsAsync(int? excludeStudentId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);

        return await ctx.Sections
            .AsNoTracking()
            .Where(s => yearId == null || s.AcademicYearId == yearId)
            .OrderBy(s => s.Grade.SortOrder).ThenBy(s => s.NameAr)
            .Select(s => new MoveTargetSection(
                s.Id,
                s.GradeId,
                s.Grade.NameAr,
                s.NameAr,
                s.Capacity,
                s.Students.Count(st => excludeStudentId == null || st.Id != excludeStudentId.Value)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
