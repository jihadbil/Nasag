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

public sealed class StudentsRepository : IStudentsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public StudentsRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<StudentListPage> SearchAsync(StudentsQuery query, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = ctx.Students
            .AsNoTracking()
            .Include(s => s.Section).ThenInclude(sec => sec.Grade)
            .Include(s => s.Guardian)
            .AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(s => s.Status == query.Status.Value);

        if (query.SectionId.HasValue)
            q = q.Where(s => s.SectionId == query.SectionId.Value);
        else if (query.GradeId.HasValue)
            q = q.Where(s => s.Section.GradeId == query.GradeId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(s =>
                s.FullName.Contains(term) ||
                s.StudentNumber.Contains(term) ||
                (s.NationalId != null && s.NationalId.Contains(term)) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                s.Guardian.FullName.Contains(term));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);

        q = query.Sort == StudentSortMode.NewestFirst
            ? q.OrderByDescending(s => s.Id)
            : q.OrderBy(s => s.FullName);

        var rows = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(s => new StudentRow(
                s.Id,
                s.StudentNumber,
                s.FullName,
                s.Gender,
                s.Status,
                s.Section.Grade.NameAr,
                s.Section.NameAr,
                s.Phone,
                s.Guardian.FullName,
                s.Guardian.Phone))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new StudentListPage(rows, total, page, size);
    }

    public async Task<StudentStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var groups = await ctx.Students
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int Get(StudentStatus s) => groups.FirstOrDefault(g => g.Status == s)?.Count ?? 0;
        var active = Get(StudentStatus.Active);
        var archived = Get(StudentStatus.Archived);
        var graduated = Get(StudentStatus.Graduated);
        return new StudentStats(active + archived + graduated, active, archived);
    }

    public async Task<StudentEditorLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .Select(g => new GradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections
            .AsNoTracking()
            .OrderBy(s => s.GradeId).ThenBy(s => s.NameAr)
            .Select(s => new SectionOption(s.Id, s.NameAr, s.GradeId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new StudentEditorLookups(grades, sections);
    }

    public async Task<StudentEditorPayload?> GetForEditAsync(int studentId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new StudentEditorPayload(
                s.Id,
                s.StudentNumber,
                s.FullName,
                s.Gender,
                s.BirthDate,
                s.NationalId,
                s.PhotoPath,
                s.PhotoBytes,
                s.Phone,
                s.Address,
                s.Notes,
                s.EnrollmentDate,
                s.Status,
                s.SectionId,
                s.GuardianId,
                s.Guardian.FullName,
                s.Guardian.Relation,
                s.Guardian.Phone,
                s.Guardian.AltPhone,
                s.Guardian.Email,
                s.Guardian.NationalId,
                s.Guardian.Occupation,
                s.Guardian.Address))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(StudentSaveModel model, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var id = await InsertCoreAsync(ctx, model, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return id;
        }).ConfigureAwait(false);
    }

    public async Task UpdateAsync(StudentSaveModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new InvalidOperationException("UpdateAsync requires Id.");

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var student = await ctx.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.Id == model.Id, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("الطالب غير موجود.");

            student.StudentNumber = model.StudentNumber.Trim();
            student.FullName = model.FullName.Trim();
            student.Gender = model.Gender;
            student.BirthDate = model.BirthDate.Date;
            student.NationalId = NullIfEmpty(model.NationalId);
            student.PhotoPath = NullIfEmpty(model.PhotoPath);
            if (model.UpdatePhoto)
                student.PhotoBytes = model.PhotoBytes;
            student.Phone = NullIfEmpty(model.Phone);
            student.Address = NullIfEmpty(model.Address);
            student.Notes = NullIfEmpty(model.Notes);
            student.EnrollmentDate = model.EnrollmentDate.Date;
            student.SectionId = model.SectionId;

            var guardian = student.Guardian;
            guardian.FullName = model.GuardianFullName.Trim();
            guardian.Relation = model.GuardianRelation;
            guardian.Phone = NullIfEmpty(model.GuardianPhone);
            guardian.AltPhone = NullIfEmpty(model.GuardianAltPhone);
            guardian.Email = NullIfEmpty(model.GuardianEmail);
            guardian.NationalId = NullIfEmpty(model.GuardianNationalId);
            guardian.Occupation = NullIfEmpty(model.GuardianOccupation);
            guardian.Address = NullIfEmpty(model.GuardianAddress);

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task SetStatusAsync(int studentId, StudentStatus status, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var student = await ctx.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("الطالب غير موجود.");
        student.Status = status;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int studentId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var student = await ctx.Students
                .AsNoTracking()
                .Select(s => new { s.Id, s.GuardianId })
                .FirstOrDefaultAsync(s => s.Id == studentId, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("الطالب غير موجود.");

            var guardianStudentCount = await ctx.Students
                .CountAsync(s => s.GuardianId == student.GuardianId, ct)
                .ConfigureAwait(false);

            await ctx.Payments
                .Where(p => p.StudentFee.StudentId == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
            await ctx.Installments
                .Where(i => i.StudentFee.StudentId == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
            await ctx.StudentFees
                .Where(f => f.StudentId == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
            await ctx.AttendanceRecords
                .Where(a => a.StudentId == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
            await ctx.Marks
                .Where(m => m.StudentId == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            var deleted = await ctx.Students
                .Where(s => s.Id == studentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            if (deleted == 0)
                throw new InvalidOperationException("Student not found.");

            if (guardianStudentCount <= 1)
            {
                await ctx.Guardians
                    .Where(g => g.Id == student.GuardianId)
                    .ExecuteDeleteAsync(ct)
                    .ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<bool> StudentNumberExistsAsync(string studentNumber, int? excludeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(studentNumber)) return false;
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var n = studentNumber.Trim();
        return excludeId is int id
            ? await ctx.Students.AnyAsync(s => s.StudentNumber == n && s.Id != id, ct).ConfigureAwait(false)
            : await ctx.Students.AnyAsync(s => s.StudentNumber == n, ct).ConfigureAwait(false);
    }

    public async Task<string> NextStudentNumberAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var lastNumeric = await ctx.Students
            .Select(s => s.StudentNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var max = 20250000;
        foreach (var raw in lastNumeric)
        {
            if (int.TryParse(raw, out var n) && n > max) max = n;
        }
        return (max + 1).ToString();
    }

    public async Task<IReadOnlyList<StudentExportRow>> GetAllForExportAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Students
            .AsNoTracking()
            .Include(s => s.Section).ThenInclude(sec => sec.Grade)
            .Include(s => s.Guardian)
            .OrderBy(s => s.FullName)
            .Select(s => new StudentExportRow
            {
                StudentNumber = s.StudentNumber,
                FullName = s.FullName,
                Gender = s.Gender == Gender.Male ? "ذكر" : "أنثى",
                BirthDate = s.BirthDate.ToString("yyyy-MM-dd"),
                NationalId = s.NationalId,
                Phone = s.Phone,
                GradeName = s.Section.Grade.NameAr,
                SectionName = s.Section.NameAr,
                EnrollmentDate = s.EnrollmentDate.ToString("yyyy-MM-dd"),
                Status = s.Status == StudentStatus.Active ? "نشط"
                          : s.Status == StudentStatus.Archived ? "مؤرشف"
                          : "متخرّج",
                Address = s.Address,
                GuardianFullName = s.Guardian.FullName,
                GuardianRelation = s.Guardian.Relation == GuardianRelation.Father ? "أب"
                                  : s.Guardian.Relation == GuardianRelation.Mother ? "أم"
                                  : s.Guardian.Relation == GuardianRelation.Brother ? "أخ"
                                  : s.Guardian.Relation == GuardianRelation.Sister ? "أخت"
                                  : s.Guardian.Relation == GuardianRelation.Uncle ? "عم/خال"
                                  : s.Guardian.Relation == GuardianRelation.Aunt ? "عمة/خالة"
                                  : s.Guardian.Relation == GuardianRelation.Grandfather ? "جد"
                                  : s.Guardian.Relation == GuardianRelation.Grandmother ? "جدة"
                                  : "أخرى",
                GuardianPhone = s.Guardian.Phone,
                GuardianAltPhone = s.Guardian.AltPhone,
                GuardianEmail = s.Guardian.Email,
                GuardianNationalId = s.Guardian.NationalId,
                GuardianOccupation = s.Guardian.Occupation,
                GuardianAddress = s.Guardian.Address,
                Notes = s.Notes,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteAllStudentsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            // Delete in dependency order: dependent rows first, then students, then orphan guardians.
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM AttendanceRecords", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Marks", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Payments", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Installments", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM StudentFees", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Students", ct).ConfigureAwait(false);
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Guardians", ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<int> BulkInsertAsync(IReadOnlyList<StudentSaveModel> models, CancellationToken ct = default)
    {
        if (models.Count == 0) return 0;

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var inserted = 0;
            foreach (var m in models)
            {
                ct.ThrowIfCancellationRequested();
                await InsertCoreAsync(ctx, m, ct).ConfigureAwait(false);
                inserted++;
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return inserted;
        }).ConfigureAwait(false);
    }

    private static async Task<int> InsertCoreAsync(NasaqDbContext ctx, StudentSaveModel model, CancellationToken ct)
    {
        var guardian = new Guardian
        {
            FullName = model.GuardianFullName.Trim(),
            Relation = model.GuardianRelation,
            Phone = NullIfEmpty(model.GuardianPhone),
            AltPhone = NullIfEmpty(model.GuardianAltPhone),
            Email = NullIfEmpty(model.GuardianEmail),
            NationalId = NullIfEmpty(model.GuardianNationalId),
            Occupation = NullIfEmpty(model.GuardianOccupation),
            Address = NullIfEmpty(model.GuardianAddress),
        };
        ctx.Guardians.Add(guardian);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        var student = new Student
        {
            StudentNumber = model.StudentNumber.Trim(),
            FullName = model.FullName.Trim(),
            Gender = model.Gender,
            BirthDate = model.BirthDate.Date,
            NationalId = NullIfEmpty(model.NationalId),
            PhotoPath = NullIfEmpty(model.PhotoPath),
            PhotoBytes = model.PhotoBytes,
            Phone = NullIfEmpty(model.Phone),
            Address = NullIfEmpty(model.Address),
            Notes = NullIfEmpty(model.Notes),
            EnrollmentDate = model.EnrollmentDate.Date,
            Status = StudentStatus.Active,
            SectionId = model.SectionId,
            GuardianId = guardian.Id,
        };
        ctx.Students.Add(student);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return student.Id;
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
