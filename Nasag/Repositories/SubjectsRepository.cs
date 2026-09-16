using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Repositories;

public sealed class SubjectsRepository : ISubjectsRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public SubjectsRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<SubjectRow>> GetAllAsync(int? gradeId, string? search, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = ctx.Subjects
            .AsNoTracking()
            .Include(s => s.Grade)
            .AsQueryable();

        if (gradeId.HasValue)
            query = query.Where(s => s.GradeId == gradeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => EF.Functions.Like(s.NameAr, $"%{term}%"));
        }

        return await query
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new SubjectRow(
                s.Id,
                s.NameAr,
                s.GradeId,
                s.Grade.NameAr,
                s.MaxMark,
                s.PassMark,
                s.Marks.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SubjectGradeOption>> GetGradesAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Grades
            .AsNoTracking()
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .Select(g => new SubjectGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(SubjectSaveModel model, CancellationToken ct = default)
    {
        var name = ValidateAndNormalize(model);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await EnsureGradeExistsAsync(ctx, model.GradeId, ct).ConfigureAwait(false);
        await EnsureNameUniqueAsync(ctx, model.GradeId, name, excludeId: null, ct).ConfigureAwait(false);

        var subject = new Subject
        {
            NameAr = name,
            GradeId = model.GradeId,
            MaxMark = model.MaxMark,
            PassMark = model.PassMark,
        };
        ctx.Subjects.Add(subject);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return subject.Id;
    }

    public async Task UpdateAsync(SubjectSaveModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new InvalidOperationException("معرّف المادة مطلوب للتعديل.");

        var name = ValidateAndNormalize(model);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var subject = await ctx.Subjects
            .FirstOrDefaultAsync(s => s.Id == model.Id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("المادة غير موجودة.");

        await EnsureGradeExistsAsync(ctx, model.GradeId, ct).ConfigureAwait(false);
        await EnsureNameUniqueAsync(ctx, model.GradeId, name, excludeId: model.Id, ct).ConfigureAwait(false);

        subject.NameAr = name;
        subject.GradeId = model.GradeId;
        subject.MaxMark = model.MaxMark;
        subject.PassMark = model.PassMark;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var hasMarks = await ctx.Marks
            .AnyAsync(m => m.SubjectId == id, ct)
            .ConfigureAwait(false);
        if (hasMarks)
            throw new InvalidOperationException(
                "لا يمكن حذف هذه المادة لوجود درجات مسجلة عليها. احذف الدرجات أولاً.");

        var deleted = await ctx.Subjects
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
        if (deleted == 0)
            throw new InvalidOperationException("المادة غير موجودة.");
    }

    public async Task<int> GetMarksCountAsync(int subjectId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Marks
            .CountAsync(m => m.SubjectId == subjectId, ct)
            .ConfigureAwait(false);
    }

    private static string ValidateAndNormalize(SubjectSaveModel model)
    {
        var name = (model.NameAr ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("اسم المادة مطلوب.");
        if (name.Length > 80)
            throw new InvalidOperationException("اسم المادة طويل جداً (الحد الأقصى 80 حرفاً).");

        if (model.GradeId <= 0)
            throw new InvalidOperationException("الرجاء اختيار الصف.");

        if (model.MaxMark <= 0m)
            throw new InvalidOperationException("الدرجة الكاملة يجب أن تكون أكبر من صفر.");
        if (model.PassMark < 0m)
            throw new InvalidOperationException("درجة النجاح يجب ألا تكون سالبة.");
        if (model.PassMark > model.MaxMark)
            throw new InvalidOperationException("درجة النجاح يجب ألا تتجاوز الدرجة الكاملة.");

        return name;
    }

    private static async Task EnsureGradeExistsAsync(NasaqDbContext ctx, int gradeId, CancellationToken ct)
    {
        var exists = await ctx.Grades.AnyAsync(g => g.Id == gradeId, ct).ConfigureAwait(false);
        if (!exists)
            throw new InvalidOperationException("الصف المختار غير موجود.");
    }

    private static async Task EnsureNameUniqueAsync(
        NasaqDbContext ctx,
        int gradeId,
        string name,
        int? excludeId,
        CancellationToken ct)
    {
        var clash = await ctx.Subjects
            .AnyAsync(s => s.GradeId == gradeId
                           && s.NameAr == name
                           && (excludeId == null || s.Id != excludeId.Value), ct)
            .ConfigureAwait(false);
        if (clash)
            throw new InvalidOperationException("توجد مادة بهذا الاسم في الصف نفسه.");
    }
}
