using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}

public sealed record DashboardSnapshot(
    DashboardStats Stats,
    IReadOnlyList<AttendanceDayPoint> AttendanceLast7Days,
    AttendanceBreakdown TodayAttendance,
    DashboardAlerts Alerts,
    IReadOnlyList<RecentActivity> RecentActivities);

public sealed record DashboardStats(
    int ActiveStudentsCount,
    int SectionsCount,
    int SubjectsCount,
    int TodayAbsentCount,
    decimal TotalCollected,
    decimal TotalRemaining);

public sealed record AttendanceDayPoint(DateTime Date, double PresentPercent, int TotalRecords);

public sealed record AttendanceBreakdown(int Present, int Absent, int Late, int Excused)
{
    public int Total => Present + Absent + Late + Excused;
    public double PresentPercent => Total == 0 ? 0 : Math.Round((double)Present * 100 / Total, 1);
}

public sealed record DashboardAlerts(
    int OverdueInstallmentsCount,
    int DueThisWeekInstallmentsCount,
    int StudentsWithoutAttendanceTodayCount);

public enum ActivityKind
{
    StudentEnrolled = 1,
    PaymentReceived = 2
}

public sealed record RecentActivity(
    ActivityKind Kind,
    string Title,
    string Subtitle,
    DateTime At);
