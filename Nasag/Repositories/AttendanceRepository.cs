using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Repositories;

public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public AttendanceRepository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<AttendanceLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .Where(g => g.Sections.Any(s => yearId == null || s.AcademicYearId == yearId))
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new AttendanceGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections
            .AsNoTracking()
            .Where(s => yearId == null || s.AcademicYearId == yearId)
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new AttendanceSectionOption(
                s.Id,
                s.NameAr,
                s.GradeId,
                s.Grade.NameAr,
                s.Students.Count(st => st.Status == StudentStatus.Active)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new AttendanceLookups(grades, sections);
    }

    public async Task<AttendanceSheet> GetAttendanceSheetAsync(int sectionId, DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        var nextDay = day.AddDays(1);
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var students = await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.FullName)
            .Select(s => new { s.Id, s.StudentNumber, s.FullName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = students.Select(s => s.Id).ToArray();
        var records = ids.Length == 0
            ? new List<AttendanceRecord>()
            : await ctx.AttendanceRecords
                .AsNoTracking()
                .Where(a => ids.Contains(a.StudentId) && a.Date >= day && a.Date < nextDay)
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var byStudent = records
            .GroupBy(r => r.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Date == day).ThenBy(r => r.Id).First());
        var rows = students
            .Select(s =>
            {
                byStudent.TryGetValue(s.Id, out var record);
                return new AttendanceStudentRow(
                    s.Id,
                    s.StudentNumber,
                    s.FullName,
                    record?.Status ?? AttendanceStatus.Present,
                    record?.Notes,
                    record?.Id);
            })
            .ToList();

        return new AttendanceSheet(sectionId, day, rows);
    }

    public async Task SaveAttendanceSheetAsync(
        int sectionId,
        DateTime date,
        IReadOnlyList<AttendanceSaveRow> rows,
        CancellationToken ct = default)
    {
        var day = date.Date;
        var nextDay = day.AddDays(1);
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var activeIds = await ctx.Students
                .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
                .Select(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var activeSet = activeIds.ToHashSet();
            var submitted = rows
                .Where(r => activeSet.Contains(r.StudentId))
                .GroupBy(r => r.StudentId)
                .Select(g => g.Last())
                .ToList();

            if (submitted.Count != rows.Select(r => r.StudentId).Distinct().Count())
                throw new InvalidOperationException("تحتوي القائمة على طلاب غير نشطين أو خارج الشعبة المحددة.");

            var ids = submitted.Select(r => r.StudentId).ToArray();
            var existing = ids.Length == 0
                ? new List<AttendanceRecord>()
                : await ctx.AttendanceRecords
                    .Where(a => ids.Contains(a.StudentId) && a.Date >= day && a.Date < nextDay)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var byStudent = new Dictionary<int, AttendanceRecord>();
            foreach (var group in existing.GroupBy(r => r.StudentId))
            {
                var ordered = group
                    .OrderByDescending(r => r.Date == day)
                    .ThenBy(r => r.Id)
                    .ToList();
                var keep = ordered[0];
                byStudent[group.Key] = keep;

                if (keep.Date != day)
                    keep.Date = day;

                if (ordered.Count > 1)
                    ctx.AttendanceRecords.RemoveRange(ordered.Skip(1));
            }

            foreach (var row in submitted)
            {
                var notes = NormalizeNotes(row.Notes);
                if (byStudent.TryGetValue(row.StudentId, out var record))
                {
                    record.Status = row.Status;
                    record.Notes = notes;
                }
                else
                {
                    ctx.AttendanceRecords.Add(new AttendanceRecord
                    {
                        StudentId = row.StudentId,
                        Date = day,
                        Status = row.Status,
                        Notes = notes
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
