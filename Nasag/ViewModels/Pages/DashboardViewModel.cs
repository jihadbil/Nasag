using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Nasag.Services;
using SkiaSharp;

namespace Nasag.ViewModels.Pages;

public sealed partial class DashboardViewModel : PageViewModel
{
    private static readonly CultureInfo ArabicCulture = new("ar-LY");
    private static readonly string[] ShortDayNames =
    {
        "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"
    };

    private readonly IDashboardService _dashboard;

    public DashboardViewModel(IDashboardService dashboard)
    {
        _dashboard = dashboard;

        AttendanceXAxes = new[]
        {
            new Axis
            {
                Labels = Array.Empty<string>(),
                LabelsPaint = new SolidColorPaint(new SKColor(0x6B, 0x7A, 0x8F)) { SKTypeface = SKTypeface.Default },
                TextSize = 12,
                SeparatorsPaint = null,
                ForceStepToMin = true,
                MinStep = 1,
                Position = AxisPosition.Start
            }
        };

        AttendanceYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = v => $"{v:0}٪",
                LabelsPaint = new SolidColorPaint(new SKColor(0x9A, 0xA6, 0xB5)) { SKTypeface = SKTypeface.Default },
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xE5, 0xE9, 0xF0)) { StrokeThickness = 1 },
                Position = AxisPosition.End
            }
        };
    }

    public override string TitleAr => "لوحة التحكم";
    public override string SubtitleAr => "نظرة عامة على بيانات المدرسة وآخر الأنشطة";

    // ===================== Stat cards =====================
    [ObservableProperty] private int _activeStudentsCount;
    [ObservableProperty] private int _sectionsCount;
    [ObservableProperty] private int _subjectsCount;
    [ObservableProperty] private int _todayAbsentCount;
    [ObservableProperty] private string _totalCollectedFormatted = "0";
    [ObservableProperty] private string _totalRemainingFormatted = "0";

    // ===================== Today attendance breakdown =====================
    [ObservableProperty] private int _todayPresent;
    [ObservableProperty] private int _todayAbsent;
    [ObservableProperty] private int _todayLate;
    [ObservableProperty] private int _todayExcused;
    [ObservableProperty] private double _todayPresentPercent;
    [ObservableProperty] private bool _hasTodayAttendance;

    // ===================== Alerts =====================
    [ObservableProperty] private int _overdueInstallments;
    [ObservableProperty] private int _dueThisWeekInstallments;
    [ObservableProperty] private int _studentsWithoutAttendanceToday;

    // ===================== Recent activities =====================
    public ObservableCollection<RecentActivity> RecentActivities { get; } = new();
    [ObservableProperty] private bool _hasRecentActivities;

    // ===================== Chart =====================
    [ObservableProperty] private bool _hasAttendanceHistory;
    public Axis[] AttendanceXAxes { get; }
    public Axis[] AttendanceYAxes { get; }

    [ObservableProperty]
    private ISeries[] _attendanceSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _todayDonutSeries = Array.Empty<ISeries>();

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        await LoadAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            StatusMessage = null;

            var snap = await _dashboard.GetSnapshotAsync(ct).ConfigureAwait(true);

            // Stats
            ActiveStudentsCount = snap.Stats.ActiveStudentsCount;
            SectionsCount = snap.Stats.SectionsCount;
            SubjectsCount = snap.Stats.SubjectsCount;
            TodayAbsentCount = snap.Stats.TodayAbsentCount;
            TotalCollectedFormatted = snap.Stats.TotalCollected.ToString("N0", ArabicCulture);
            TotalRemainingFormatted = snap.Stats.TotalRemaining.ToString("N0", ArabicCulture);

            // Today breakdown
            TodayPresent = snap.TodayAttendance.Present;
            TodayAbsent = snap.TodayAttendance.Absent;
            TodayLate = snap.TodayAttendance.Late;
            TodayExcused = snap.TodayAttendance.Excused;
            TodayPresentPercent = snap.TodayAttendance.PresentPercent;
            HasTodayAttendance = snap.TodayAttendance.Total > 0;

            // Alerts
            OverdueInstallments = snap.Alerts.OverdueInstallmentsCount;
            DueThisWeekInstallments = snap.Alerts.DueThisWeekInstallmentsCount;
            StudentsWithoutAttendanceToday = snap.Alerts.StudentsWithoutAttendanceTodayCount;

            // Recent activities
            RecentActivities.Clear();
            foreach (var a in snap.RecentActivities) RecentActivities.Add(a);
            HasRecentActivities = RecentActivities.Count > 0;

            // Attendance line chart (last 7 days)
            UpdateAttendanceChart(snap.AttendanceLast7Days);

            // Today donut
            UpdateTodayDonut(snap.TodayAttendance);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل بيانات لوحة التحكم: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateAttendanceChart(IReadOnlyList<AttendanceDayPoint> points)
    {
        var hasAny = points.Any(p => p.TotalRecords > 0);
        HasAttendanceHistory = hasAny;

        var labels = points.Select(p => ShortDayNames[(int)p.Date.DayOfWeek]).ToArray();
        AttendanceXAxes[0].Labels = labels;

        var teal = new SKColor(0x1F, 0xB5, 0xA8);
        var tealSoft = new SKColor(0x1F, 0xB5, 0xA8, 40);

        AttendanceSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = points.Select(p => p.PresentPercent).ToArray(),
                Name = "نسبة الحضور",
                GeometrySize = 10,
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(teal) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(tealSoft),
                GeometryStroke = new SolidColorPaint(teal) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(SKColors.White)
            }
        };
    }

    private void UpdateTodayDonut(AttendanceBreakdown b)
    {
        if (b.Total == 0)
        {
            // Single neutral slice so the donut renders even with no data
            TodayDonutSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new double[] { 1 },
                    Name = "لا توجد بيانات",
                    Fill = new SolidColorPaint(new SKColor(0xE5, 0xE9, 0xF0)),
                    InnerRadius = 60,
                    DataLabelsPaint = null,
                    IsHoverable = false
                }
            };
            return;
        }

        TodayDonutSeries = new ISeries[]
        {
            BuildSlice("حاضر", b.Present, new SKColor(0x22, 0xC5, 0x5E)),
            BuildSlice("غائب", b.Absent, new SKColor(0xEF, 0x44, 0x44)),
            BuildSlice("متأخر", b.Late, new SKColor(0xF5, 0x9E, 0x0B)),
            BuildSlice("إجازة", b.Excused, new SKColor(0x3B, 0x82, 0xF6))
        };
    }

    private static PieSeries<double> BuildSlice(string name, int value, SKColor color)
        => new()
        {
            Values = new double[] { value },
            Name = name,
            Fill = new SolidColorPaint(color),
            InnerRadius = 60,
            DataLabelsPaint = null,
            IsHoverable = true
        };
}
