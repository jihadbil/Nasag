using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Services;

public sealed class SettingsRepository : ISettingsRepository
{
    private const string DefaultSchoolNameAr = "مدرسة النور الأهلية";

    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly ICurrentUserService _currentUser;

    public SettingsRepository(IDbContextFactory<NasaqDbContext> factory, ICurrentUserService currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<SchoolSettings> GetOrCreateAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Tracked load so callers that bind to the entity can flow it through SaveAsync
        // unchanged. The page reloads after save anyway, so the tracking lifetime is
        // bounded to a single call.
        var existing = await ctx.SchoolSettings
            .Include(s => s.CurrentAcademicYear)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null) return existing;

        var created = new SchoolSettings { NameAr = DefaultSchoolNameAr };
        ctx.SchoolSettings.Add(created);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return created;
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAcademicYearsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .ThenByDescending(y => y.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(SchoolSettings settings, CancellationToken ct = default)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        EnsurePermission(Permission.ManageSettings);

        ValidateSchoolFields(settings);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var row = await ctx.SchoolSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null)
        {
            row = new SchoolSettings { NameAr = DefaultSchoolNameAr };
            ctx.SchoolSettings.Add(row);
        }

        row.NameAr = settings.NameAr?.Trim() ?? string.Empty;
        row.Address = NormalizeOptional(settings.Address);
        row.Phone = NormalizeOptional(settings.Phone);
        row.Email = NormalizeOptional(settings.Email);
        row.Website = NormalizeOptional(settings.Website);
        row.PrincipalName = NormalizeOptional(settings.PrincipalName);
        row.LogoBytes = settings.LogoBytes;

        // CurrentAcademicYearId is also editable through this save (the page presents
        // them in one form). If a caller passes 0 we treat it as null (no selection).
        if (settings.CurrentAcademicYearId.HasValue && settings.CurrentAcademicYearId.Value > 0)
        {
            var yearExists = await ctx.AcademicYears
                .AsNoTracking()
                .AnyAsync(y => y.Id == settings.CurrentAcademicYearId.Value, ct)
                .ConfigureAwait(false);
            if (!yearExists)
                throw new InvalidOperationException("السنة الدراسية المختارة غير موجودة.");

            row.CurrentAcademicYearId = settings.CurrentAcademicYearId;
        }
        else
        {
            row.CurrentAcademicYearId = null;
        }

        // Single SaveChanges — EF Core manages the transaction implicitly; no need
        // for ExecutionStrategy here.
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<AcademicYear> CreateAcademicYearAsync(string nameAr, DateTime start, DateTime end, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageSettings);
        ValidateYear(nameAr, start, end);

        var name = nameAr.Trim();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var duplicate = await ctx.AcademicYears
            .AsNoTracking()
            .AnyAsync(y => y.NameAr == name, ct)
            .ConfigureAwait(false);
        if (duplicate)
            throw new InvalidOperationException("توجد سنة دراسية بنفس الاسم.");

        var entity = new AcademicYear
        {
            NameAr = name,
            StartDate = start.Date,
            EndDate = end.Date,
            IsActive = true
        };
        ctx.AcademicYears.Add(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAcademicYearAsync(int yearId, string nameAr, DateTime start, DateTime end, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageSettings);
        ValidateYear(nameAr, start, end);

        var name = nameAr.Trim();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var year = await ctx.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == yearId, ct)
            .ConfigureAwait(false);
        if (year is null)
            throw new InvalidOperationException("السنة الدراسية غير موجودة.");

        var duplicate = await ctx.AcademicYears
            .AsNoTracking()
            .AnyAsync(y => y.NameAr == name && y.Id != yearId, ct)
            .ConfigureAwait(false);
        if (duplicate)
            throw new InvalidOperationException("توجد سنة دراسية بنفس الاسم.");

        year.NameAr = name;
        year.StartDate = start.Date;
        year.EndDate = end.Date;

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetCurrentAcademicYearAsync(int yearId, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageSettings);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var yearExists = await ctx.AcademicYears
            .AsNoTracking()
            .AnyAsync(y => y.Id == yearId, ct)
            .ConfigureAwait(false);
        if (!yearExists)
            throw new InvalidOperationException("السنة الدراسية المختارة غير موجودة.");

        var row = await ctx.SchoolSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null)
        {
            row = new SchoolSettings { NameAr = DefaultSchoolNameAr, CurrentAcademicYearId = yearId };
            ctx.SchoolSettings.Add(row);
        }
        else
        {
            row.CurrentAcademicYearId = yearId;
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAcademicYearAsync(int yearId, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageSettings);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var year = await ctx.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == yearId, ct)
            .ConfigureAwait(false);
        if (year is null)
            throw new InvalidOperationException("السنة الدراسية غير موجودة.");

        // Refuse delete when this is the school's current academic year — otherwise
        // many parts of the app (Fees lookups, Classes screen, Marks/Exams) would
        // dangle. The user must change the current year first.
        var currentId = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => s.CurrentAcademicYearId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (currentId == yearId)
            throw new InvalidOperationException("لا يمكن حذف السنة الدراسية الحالية. اختر سنة أخرى كحالية ثم حاول الحذف.");

        // Refuse delete when any aggregate still references the year — there's no
        // safe cascade path that wouldn't silently destroy classes, exams, or fee
        // plans associated with the year.
        var hasSections = await ctx.Sections.AsNoTracking().AnyAsync(s => s.AcademicYearId == yearId, ct).ConfigureAwait(false);
        if (hasSections)
            throw new InvalidOperationException("لا يمكن حذف السنة الدراسية لأن هناك شعب مرتبطة بها.");

        var hasExams = await ctx.Exams.AsNoTracking().AnyAsync(e => e.AcademicYearId == yearId, ct).ConfigureAwait(false);
        if (hasExams)
            throw new InvalidOperationException("لا يمكن حذف السنة الدراسية لأن هناك امتحانات مرتبطة بها.");

        var hasFeePlans = await ctx.FeePlans.AsNoTracking().AnyAsync(p => p.AcademicYearId == yearId, ct).ConfigureAwait(false);
        if (hasFeePlans)
            throw new InvalidOperationException("لا يمكن حذف السنة الدراسية لأن هناك خطط رسوم مرتبطة بها.");

        ctx.AcademicYears.Remove(year);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void ValidateSchoolFields(SchoolSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.NameAr))
            throw new InvalidOperationException("اسم المدرسة مطلوب.");
        if (s.NameAr.Trim().Length > 200)
            throw new InvalidOperationException("اسم المدرسة طويل جداً.");
        if (!string.IsNullOrWhiteSpace(s.Email) && s.Email!.Length > 200)
            throw new InvalidOperationException("البريد الإلكتروني طويل جداً.");
    }

    private static void ValidateYear(string nameAr, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new InvalidOperationException("اسم السنة الدراسية مطلوب.");
        if (nameAr.Trim().Length > 60)
            throw new InvalidOperationException("اسم السنة الدراسية طويل جداً.");
        if (start == default || end == default)
            throw new InvalidOperationException("تاريخا البداية والنهاية مطلوبان.");
        if (end.Date <= start.Date)
            throw new InvalidOperationException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية.");
    }

    private static string? NormalizeOptional(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length <= 300 ? text : text[..300];
    }

    private void EnsurePermission(Permission required)
    {
        if (!_currentUser.HasPermission(required))
            throw new UnauthorizedAccessException("ليس لديك صلاحية تعديل إعدادات المدرسة.");
    }
}
