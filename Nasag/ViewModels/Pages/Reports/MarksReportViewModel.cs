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

public sealed partial class MarksReportViewModel : ObservableObject
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

    public MarksReportViewModel(
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
    }

    public Action<RecentReportEntry>? OnReportGenerated { get; set; }

    public ObservableCollection<ReportGradeOption> Grades { get; } = new();
    public ObservableCollection<ReportSectionOption> AvailableSections { get; } = new();
    public ObservableCollection<ReportExamOption> Exams { get; } = new();

    [ObservableProperty] private ReportGradeOption? _selectedGrade;
    [ObservableProperty] private ReportSectionOption? _selectedSection;
    [ObservableProperty] private ReportExamOption? _selectedExam; // null = all exams aggregate
    [ObservableProperty] private bool _isBusy;

    public bool CanRun => SelectedGrade is not null && SelectedSection is not null && !IsBusy;

    partial void OnSelectedGradeChanged(ReportGradeOption? value)
    {
        RefreshSectionsForGrade();
        OnPropertyChanged(nameof(CanRun));
        NotifyActionStates();
    }

    partial void OnSelectedSectionChanged(ReportSectionOption? value)
    {
        OnPropertyChanged(nameof(CanRun));
        NotifyActionStates();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRun));
        NotifyActionStates();
    }

    private void NotifyActionStates()
    {
        PreviewCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
        ExportExcelCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSectionsForGrade()
    {
        AvailableSections.Clear();
        if (SelectedGrade is null)
        {
            // For marks, sections are tied to a grade — show none until a grade is picked.
            SelectedSection = null;
            return;
        }
        foreach (var s in _allSections.Where(s => s.GradeId == SelectedGrade.Id))
            AvailableSections.Add(s);
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
            Exams.Clear();
            foreach (var e in lookups.Exams) Exams.Add(e);
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

    private bool ValidateRequired()
    {
        if (SelectedGrade is null)
        {
            _toasts.Warning("اختر الصف", "الرجاء اختيار الصف لإنشاء كشف الدرجات.");
            return false;
        }
        if (SelectedSection is null)
        {
            _toasts.Warning("اختر الشعبة", "الرجاء اختيار الشعبة لإنشاء كشف الدرجات.");
            return false;
        }
        return true;
    }

    private MarksReportQuery BuildQuery() =>
        new(SelectedGrade!.Id, SelectedSection!.Id, SelectedExam?.Id);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PreviewAsync()
    {
        if (!_users.HasPermission(Permission.ManageReports))
        {
            _toasts.Warning("لا تملك صلاحية", "ليست لديك صلاحية إنشاء هذا التقرير.");
            return;
        }
        if (!ValidateRequired()) return;
        try
        {
            IsBusy = true;
            var result = await _repo.GetMarksReportAsync(BuildQuery()).ConfigureAwait(true);
            var doc = MarksReportDocument.Build(result);
            PrintService.PreviewAndPrint(doc, "كشف الدرجات");
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
        if (!ValidateRequired()) return;
        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"كشف-الدرجات-{DateTime.Now:yyyy-MM-dd}.pdf",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            var result = await _repo.GetMarksReportAsync(BuildQuery()).ConfigureAwait(true);
            await _pdf.SaveMarksAsync(dlg.FileName, result).ConfigureAwait(true);
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
        if (!ValidateRequired()) return;
        var dlg = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"كشف-الدرجات-{DateTime.Now:yyyy-MM-dd}.xlsx",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            var result = await _repo.GetMarksReportAsync(BuildQuery()).ConfigureAwait(true);
            await _excel.ExportMarksReportAsync(dlg.FileName, result).ConfigureAwait(true);
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

    private void ReportRecent(MarksReportResult r)
    {
        var desc = $"{r.GradeNameAr} • {r.SectionNameAr} • {r.ExamNameAr} • {r.Totals.StudentCount} طالب";
        OnReportGenerated?.Invoke(new RecentReportEntry(
            Guid.NewGuid(),
            RecentReportKind.Marks,
            "كشف الدرجات",
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
