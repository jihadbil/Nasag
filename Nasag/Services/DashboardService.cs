using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;

namespace Nasag.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public DashboardService(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var today = DateTime.UtcNow.Date;
        var sevenDaysAgo = today.AddDays(-6);
        var endOfWeek = today.AddDays(7);

        var stats = await BuildStatsAsync(ctx, today, ct).ConfigureAwait(false);
        var attendance7 = await BuildAttendance7Async(ctx, sevenDaysAgo, today, ct).ConfigureAwait(false);
        var todayBreakdown = await BuildTodayBreakdownAsync(ctx, today, ct).ConfigureAwait(false);
        var alerts = await BuildAlertsAsync(ctx, today, endOfWeek, stats.ActiveStudentsCount, ct).ConfigureAwait(false);
        var recents = await BuildRecentActivitiesAsync(ctx, ct).ConfigureAwait(false);

        return new DashboardSnapshot(stats, attendance7, todayBreakdown, alerts, recents);
    }

    private static async Task<DashboardStats> BuildStatsAsync(NasaqDbContext ctx, DateTime today, CancellationToken ct)
    {
        var activeStudents = await ctx.Students
            .CountAsync(s => s.Status == StudentStatus.Active, ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections.CountAsync(ct).ConfigureAwait(false);
        var subjects = await ctx.Subjects.CountAsync(ct).ConfigureAwait(false);

        var tomorrow = today.AddDays(1);
        var todayAbsent = await ctx.AttendanceRecords
            .CountAsync(a => a.Date >= today && a.Date < tomorrow && a.Status == AttendanceStatus.Absent, ct)
            .ConfigureAwait(false);

        var totalCollected = await ctx.StudentFees
            .SumAsync(f => (decimal?)f.PaidAmount, ct)
            .ConfigureAwait(false) ?? 0m;

        var totalRemaining = await ctx.StudentFees
            .SumAsync(f => (decimal?)(f.TotalAmount - f.PaidAmount), ct)
            .ConfigureAwait(false) ?? 0m;

        return new DashboardStats(activeStudents, sections, subjects, todayAbsent, totalCollected, totalRemaining);
    }

    private static async Task<IReadOnlyList<AttendanceDayPoint>> BuildAttendance7Async(
        NasaqDbContext ctx, DateTime startInclusive, DateTime endInclusive, CancellationToken ct)
    {
        var endExclusive = endInclusive.AddDays(1);
        var grouped = await ctx.AttendanceRecords
            .Where(a => a.Date >= startInclusive && a.Date < endExclusive)
            .GroupBy(a => a.Date.Date)
            .Select(g => new
            {
                Day = g.Key,
                Total = g.Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byDay = grouped.ToDictionary(x => x.Day, x => x);
        var points = new List<AttendanceDayPoint>(7);
        for (var i = 0; i < 7; i++)
        {
            var d = startInclusive.AddDays(i);
            if (byDay.TryGetValue(d, out var bucket) && bucket.Total > 0)
            {
                var pct = Math.Round((double)bucket.Present * 100 / bucket.Total, 1);
                points.Add(new AttendanceDayPoint(d, pct, bucket.Total));
            }
            else
            {
                points.Add(new AttendanceDayPoint(d, 0, 0));
            }
        }
        return points;
    }

    private static async Task<AttendanceBreakdown> BuildTodayBreakdownAsync(
        NasaqDbContext ctx, DateTime today, CancellationToken ct)
    {
        var tomorrow = today.AddDays(1);
        var rows = await ctx.AttendanceRecords
            .Where(a => a.Date >= today && a.Date < tomorrow)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int Get(AttendanceStatus s) => rows.FirstOrDefault(r => r.Status == s)?.Count ?? 0;
        return new AttendanceBreakdown(
            Present: Get(AttendanceStatus.Present),
            Absent: Get(AttendanceStatus.Absent),
            Late: Get(AttendanceStatus.Late),
            Excused: Get(AttendanceStatus.Excused));
    }

    private static async Task<DashboardAlerts> BuildAlertsAsync(
        NasaqDbContext ctx, DateTime today, DateTime endOfWeekExclusive, int activeStudents, CancellationToken ct)
    {
        var overdue = await ctx.Installments
            .CountAsync(i => i.DueDate < today && i.Status != InstallmentStatus.Paid, ct)
            .ConfigureAwait(false);

        var dueThisWeek = await ctx.Installments
            .CountAsync(i => i.DueDate >= today && i.DueDate < endOfWeekExclusive && i.Status != InstallmentStatus.Paid, ct)
            .ConfigureAwait(false);

        var tomorrow = today.AddDays(1);
        var recordedToday = await ctx.AttendanceRecords
            .Where(a => a.Date >= today && a.Date < tomorrow)
            .Select(a => a.StudentId)
            .Distinct()
            .CountAsync(ct)
            .ConfigureAwait(false);

        var withoutAttendance = Math.Max(0, activeStudents - recordedToday);

        return new DashboardAlerts(overdue, dueThisWeek, withoutAttendance);
    }

    private static async Task<IReadOnlyList<RecentActivity>> BuildRecentActivitiesAsync(
        NasaqDbContext ctx, CancellationToken ct)
    {
        var enrollments = await ctx.Students
            .OrderByDescending(s => s.EnrollmentDate)
            .Take(5)
            .Select(s => new
            {
                s.FullName,
                s.EnrollmentDate,
                SectionName = s.Section.NameAr,
                GradeName = s.Section.Grade.NameAr
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var payments = await ctx.Payments
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .Select(p => new
            {
                p.Amount,
                p.PaymentDate,
                p.ReceiptNumber,
                StudentName = p.StudentFee.Student.FullName
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var list = new List<RecentActivity>();
        foreach (var s in enrollments)
        {
            list.Add(new RecentActivity(
                ActivityKind.StudentEnrolled,
                $"تسجيل طالب جديد: {s.FullName}",
                $"{s.GradeName} — شعبة {s.SectionName}",
                s.EnrollmentDate));
        }
        foreach (var p in payments)
        {
            list.Add(new RecentActivity(
                ActivityKind.PaymentReceived,
                $"تسجيل دفعة من {p.StudentName}",
                $"سند رقم {p.ReceiptNumber} — {p.Amount:N0} ر.ي",
                p.PaymentDate));
        }

        return list
            .OrderByDescending(a => a.At)
            .Take(8)
            .ToList();
    }
}
