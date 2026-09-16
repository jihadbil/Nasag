using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Services.Printing;
using Nasag.Services.Printing.Reports;
using Nasag.Services.Reports;

namespace Nasag.ViewModels.Pages.Reports;

public sealed partial class AttendanceReportViewModel : ObservableObject
{
    private readonly IReportsRepository _repo;
    private readonly IReportPdfService _pdf;
    private readonly IExcelService _excel;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly ICurrentUserService _users;
    private bool _initialized;
    private IReadOnlyList<ReportSectionOption> _allSections = Array.Empty<ReportSectionOption>();

    public AttendanceReportViewModel(
        IReportsRepository repo,
        IReportPdfService pdf,
        IExcelService excel,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        ICurrentUserService users)
    {
        _repo = repo;
        _pdf = pdf;
        _excel = excel;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _users = users;

        DateTo = DateTime.Today;
        DateFrom = DateTime.Today.AddDays(-30);
    }

    public Action<RecentReportEntry>? OnReportGenerated { get; set; }

    public ObservableCollection<ReportGradeOption> Grades { get; } = new();
    public ObservableCollection<ReportSectionOption> AvailableSections { get; } = new();

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private ReportGradeOption? _selectedGrade;
    [ObservableProperty] private ReportSectionOption? _selectedSection;
    [ObservableProperty] private bool _isBusy;

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        PreviewCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
        ExportExcelCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGradeChanged(ReportGradeOption? value) => RefreshSectionsForGrade();

    private void RefreshSectionsForGrade()
    {
        AvailableSections.Clear();
        if (SelectedGrade is null)
        {
            foreach (var s in _allSections) AvailableSections.Add(s);
        }
        else
        {
            foreach (var s in _allSections.Where(s => s.GradeId == SelectedGrade.Id))
                AvailableSections.Add(s);
        }
        if (SelectedSection is not null && !AvailableSections.Contains(SelectedSection))
            SelectedSection = null;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        try
        {
            IsBusy = true;
            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);
            Grades.Clear();
            foreach (var g in lookups.Grades) Grades.Add(g);
            _allSections = lookups.Sections;
            RefreshSectionsForGrade();
            _initialized = true;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحميل بيانات التصفية", ex.Message, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateDates(out DateTime from, out DateTime to)
    {
        from = DateFrom ?? DateTime.Today.AddDays(-30);
        to = DateTo ?? DateTime.Today;
        if (to.Date < from.Date)
        {
            _toasts.Warning("نطاق تاريخ غير صالح", "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.");
            return false;
        }
        return true;
    }

    private AttendanceReportQuery BuildQuery(DateTime from, DateTime to) =>
        new(from.Date, to.Date, SelectedGrade?.Id, SelectedSection?.Id);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PreviewAsync()
    {
        if (!_users.HasPermission(Permission.ManageReports))
        {
            _toasts.Warning("لا تملك صلاحية", "ليست لديك صلاحية إنشاء هذا التقرير.");
            return;
        }
        if (!ValidateDates(out var f, out var t)) return;
        try
        {
            IsBusy = true;
            var result = await _repo.GetAttendanceReportAsync(BuildQuery(f, t)).ConfigureAwait(true);
            var doc = AttendanceReportDocument.Build(result);
            PrintService.PreviewAndPrint(doc, "كشف الحضور");
            ReportRecent(result);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إنشاء التقرير", ex.Message, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportPdfAsync()
    {
        if (!_users.HasPermission(Permission.ManageReports))
        {
            _toasts.Warning("لا تملك صلاحية", "ليست لديك صلاحية تصدير التقارير.");
            return;
        }
        if (!ValidateDates(out var f, out var t)) return;
        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"كشف-الحضور-{DateTime.Now:yyyy-MM-dd}.pdf",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            var result = await _repo.GetAttendanceReportAsync(BuildQuery(f, t)).ConfigureAwait(true);
            await _pdf.SaveAttendanceAsync(dlg.FileName, result).ConfigureAwait(true);
            _toasts.Success("تم التصدير", $"حُفظ التقرير: {Path.GetFileName(dlg.FileName)}");
            TryOpenFile(dlg.FileName);
            ReportRecent(result);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تصدير PDF", ex.Message, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportExcelAsync()
    {
        if (!_users.HasPermission(Permission.ManageReports))
        {
            _toasts.Warning("لا تملك صلاحية", "ليست لديك صلاحية تصدير التقارير.");
            return;
        }
        if (!ValidateDates(out var f, out var t)) return;
        var dlg = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"كشف-الحضور-{DateTime.Now:yyyy-MM-dd}.xlsx",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            var result = await _repo.GetAttendanceReportAsync(BuildQuery(f, t)).ConfigureAwait(true);
            await _excel.ExportAttendanceReportAsync(dlg.FileName, result).ConfigureAwait(true);
            _toasts.Success("تم التصدير", $"حُفظ ملف Excel: {Path.GetFileName(dlg.FileName)}");
            TryOpenFile(dlg.FileName);
            ReportRecent(result);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تصدير Excel", ex.Message, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReportRecent(AttendanceReportResult r)
    {
        var grade = string.IsNullOrEmpty(r.GradeNameAr) ? "كل الصفوف" : r.GradeNameAr;
        var section = string.IsNullOrEmpty(r.SectionNameAr) ? "كل الشعب" : r.SectionNameAr;
        var desc = $"من {r.DateFrom:yyyy/MM/dd} إلى {r.DateTo:yyyy/MM/dd} • {grade} • {section} • {r.Totals.StudentCount} طالب";
        OnReportGenerated?.Invoke(new RecentReportEntry(
            Guid.NewGuid(),
            RecentReportKind.Attendance,
            "كشف الحضور",
            desc,
            _users.DisplayName,
            DateTime.Now));
    }

    private void TryOpenFile(string path)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* best-effort */ }
    }
}
