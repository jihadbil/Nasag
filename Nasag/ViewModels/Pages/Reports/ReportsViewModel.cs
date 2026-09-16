using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Services.Reports;
using Nasag.Views.Pages.Reports;

namespace Nasag.ViewModels.Pages.Reports;

public enum RecentReportKind
{
    Students,
    Attendance,
    Marks,
    Fees
}

public sealed record RecentReportEntry(
    Guid Id,
    RecentReportKind Kind,
    string TitleAr,
    string DescriptionAr,
    string UserName,
    DateTime CreatedAt);

/// <summary>
/// Phase 11 Reports hub. Hosts 4 report cards and a session-only recent reports list.
/// Each card opens a dedicated dialog (Students/Attendance/Marks/Fees) which performs
/// preview/print, PDF export, or Excel export. Successful operations append to RecentReports.
/// </summary>
public sealed partial class ReportsViewModel : PageViewModel
{
    private readonly IReportsRepository _repo;
    private readonly IReportPdfService _pdf;
    private readonly IExcelService _excel;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly ICurrentUserService _users;
    private readonly IServiceProvider _services;

    public ReportsViewModel(
        IReportsRepository repo,
        IReportPdfService pdf,
        IExcelService excel,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        ICurrentUserService users,
        IServiceProvider services)
    {
        _repo = repo;
        _pdf = pdf;
        _excel = excel;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _users = users;
        _services = services;

        RecentReports.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRecentReports));
            OnPropertyChanged(nameof(IsRecentEmpty));
        };
    }

    public override string TitleAr => "مركز التقارير";
    public override string SubtitleAr => "استخراج التقارير المختلفة وطباعتها";

    public ObservableCollection<RecentReportEntry> RecentReports { get; } = new();

    public bool HasRecentReports => RecentReports.Count > 0;
    public bool IsRecentEmpty => RecentReports.Count == 0;

    // ----- Card command handlers -----

    [RelayCommand]
    private async Task OpenStudentsReportAsync()
    {
        if (!EnsurePermission()) return;
        try
        {
            var vm = _services.GetService<StudentsReportViewModel>() ?? ActivateStudentsVm();
            vm.OnReportGenerated = AppendRecent;
            await vm.InitializeAsync().ConfigureAwait(true);
            var dlg = new StudentsReportDialog { DataContext = vm, Owner = ActiveOwner() };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح تقرير الطلاب", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task OpenAttendanceReportAsync()
    {
        if (!EnsurePermission()) return;
        try
        {
            var vm = _services.GetService<AttendanceReportViewModel>() ?? ActivateAttendanceVm();
            vm.OnReportGenerated = AppendRecent;
            await vm.InitializeAsync().ConfigureAwait(true);
            var dlg = new AttendanceReportDialog { DataContext = vm, Owner = ActiveOwner() };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح كشف الحضور", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task OpenMarksReportAsync()
    {
        if (!EnsurePermission()) return;
        try
        {
            var vm = _services.GetService<MarksReportViewModel>() ?? ActivateMarksVm();
            vm.OnReportGenerated = AppendRecent;
            await vm.InitializeAsync().ConfigureAwait(true);
            var dlg = new MarksReportDialog { DataContext = vm, Owner = ActiveOwner() };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح كشف الدرجات", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task OpenFeesReportAsync()
    {
        if (!EnsurePermission()) return;
        try
        {
            var vm = _services.GetService<FeesReportViewModel>() ?? ActivateFeesVm();
            vm.OnReportGenerated = AppendRecent;
            await vm.InitializeAsync().ConfigureAwait(true);
            var dlg = new FeesReportDialog { DataContext = vm, Owner = ActiveOwner() };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح كشف الرسوم", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task ClearRecentAsync()
    {
        if (RecentReports.Count == 0) return;
        var ok = await _dialogs.ConfirmAsync(
            "مسح القائمة",
            "هل تريد مسح قائمة آخر التقارير؟ هذا الإجراء يحذف العناصر من الجلسة الحالية فقط.",
            "مسح",
            "إلغاء");
        if (ok) RecentReports.Clear();
    }

    [RelayCommand]
    private void RemoveRecent(RecentReportEntry? entry)
    {
        if (entry is null) return;
        RecentReports.Remove(entry);
    }

    [RelayCommand]
    private async Task ReopenRecentAsync(RecentReportEntry? entry)
    {
        if (entry is null) return;
        switch (entry.Kind)
        {
            case RecentReportKind.Students: await OpenStudentsReportAsync(); break;
            case RecentReportKind.Attendance: await OpenAttendanceReportAsync(); break;
            case RecentReportKind.Marks: await OpenMarksReportAsync(); break;
            case RecentReportKind.Fees: await OpenFeesReportAsync(); break;
        }
    }

    // ----- Helpers -----

    private void AppendRecent(RecentReportEntry entry)
    {
        // Keep newest at the top, cap at 50 to avoid unbounded growth in long sessions.
        RecentReports.Insert(0, entry);
        while (RecentReports.Count > 50) RecentReports.RemoveAt(RecentReports.Count - 1);
    }

    private bool EnsurePermission()
    {
        if (_users.HasPermission(Permission.ManageReports)) return true;
        _toasts.Warning("لا تملك صلاحية", "ليست لديك صلاحية إنشاء التقارير.");
        return false;
    }

    private static Window? ActiveOwner()
    {
        if (Application.Current is null) return null;
        foreach (Window w in Application.Current.Windows)
            if (w.IsActive) return w;
        return Application.Current.MainWindow;
    }

    // Fallback factories — used only when DI hasn't registered the dialog VMs.
    // This lets the page work even before the main thread wires DI for the new VMs.
    private StudentsReportViewModel ActivateStudentsVm() =>
        new(_repo, _pdf, _excel, _dialogs, _toasts, _errors, _users);

    private AttendanceReportViewModel ActivateAttendanceVm() =>
        new(_repo, _pdf, _excel, _dialogs, _toasts, _errors, _users);

    private MarksReportViewModel ActivateMarksVm() =>
        new(_repo, _pdf, _excel, _dialogs, _toasts, _errors, _users);

    private FeesReportViewModel ActivateFeesVm() =>
        new(_repo, _pdf, _excel, _dialogs, _toasts, _errors, _users);
}
