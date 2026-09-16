using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Repositories;

public sealed class ExamsRepository : IExamsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public ExamsRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<ExamRow>> GetAllAsync(
        int? academicYearId,
        string? search,
        CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = ctx.Exams
            .AsNoTracking()
            .Include(e => e.AcademicYear)
            .AsQueryable();

        if (academicYearId.HasValue)
            query = query.Where(e => e.AcademicYearId == academicYearId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => EF.Functions.Like(e.NameAr, $"%{term}%"));
        }

        return await query
            .OrderByDescending(e => e.AcademicYear.StartDate)
            .ThenBy(e => e.NameAr)
            .Select(e => new ExamRow(
                e.Id,
                e.NameAr,
                e.AcademicYearId,
                e.AcademicYear.NameAr,
                e.Weight,
                e.Marks.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExamYearOption>> GetYearsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => new ExamYearOption(y.Id, y.NameAr, y.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int?> GetCurrentAcademicYearIdAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await GetCurrentYearIdInternalAsync(ctx, ct).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(ExamSaveModel model, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ValidateModel(model);

        var yearExists = await ctx.AcademicYears
            .AnyAsync(y => y.Id == model.AcademicYearId, ct)
            .ConfigureAwait(false);
        if (!yearExists)
            throw new InvalidOperationException("السنة الدراسية المختارة غير موجودة.");

        var name = model.NameAr.Trim();
        var clash = await ctx.Exams
            .AnyAsync(e => e.AcademicYearId == model.AcademicYearId && e.NameAr == name, ct)
            .ConfigureAwait(false);
        if (clash)
            throw new InvalidOperationException("يوجد امتحان بهذا الاسم في السنة الدراسية المختارة.");

        var exam = new Exam
        {
            NameAr = name,
            AcademicYearId = model.AcademicYearId,
            Weight = model.Weight,
        };
        ctx.Exams.Add(exam);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return exam.Id;
    }

    public async Task UpdateAsync(ExamSaveModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new InvalidOperationException("معرّف الامتحان مطلوب للتعديل.");

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ValidateModel(model);

        var exam = await ctx.Exams.FirstOrDefaultAsync(e => e.Id == model.Id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("الامتحان غير موجود.");

        var yearExists = await ctx.AcademicYears
            .AnyAsync(y => y.Id == model.AcademicYearId, ct)
            .ConfigureAwait(false);
        if (!yearExists)
            throw new InvalidOperationException("السنة الدراسية المختارة غير موجودة.");

        var name = model.NameAr.Trim();
        var clash = await ctx.Exams
            .AnyAsync(e => e.Id != model.Id &&
                           e.AcademicYearId == model.AcademicYearId &&
                           e.NameAr == name, ct)
            .ConfigureAwait(false);
        if (clash)
            throw new InvalidOperationException("يوجد امتحان آخر بهذا الاسم في السنة الدراسية المختارة.");

        exam.NameAr = name;
        exam.AcademicYearId = model.AcademicYearId;
        exam.Weight = model.Weight;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var hasMarks = await ctx.Marks.AnyAsync(m => m.ExamId == id, ct).ConfigureAwait(false);
        if (hasMarks)
            throw new InvalidOperationException(
                "لا يمكن حذف هذا الامتحان لوجود درجات مسجلة عليه. احذف الدرجات أولاً.");

        var deleted = await ctx.Exams
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        if (deleted == 0)
            throw new InvalidOperationException("الامتحان غير موجود.");
    }

    private static void ValidateModel(ExamSaveModel model)
    {
        var name = (model.NameAr ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("الرجاء إدخال اسم الامتحان.");
        if (name.Length > 80)
            throw new InvalidOperationException("اسم الامتحان طويل جداً (الحد الأقصى 80 حرفاً).");
        if (model.AcademicYearId <= 0)
            throw new InvalidOperationException("الرجاء اختيار السنة الدراسية.");
        if (model.Weight < 0.1m || model.Weight > 10m)
            throw new InvalidOperationException("الوزن يجب أن يكون بين 0.1 و 10.");
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
}
