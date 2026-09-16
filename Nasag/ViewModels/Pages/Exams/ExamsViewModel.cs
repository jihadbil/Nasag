using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Views.Pages.Exams.Dialogs;

namespace Nasag.ViewModels.Pages.Exams;

public sealed partial class ExamsViewModel : PageViewModel
{
    private readonly IExamsRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;

    private readonly List<ExamYearOption> _allYears = new();
    private CancellationTokenSource? _searchCts;
    private bool _isInitializing = true;
    private bool _reloadInFlight;

    /// <summary>
    /// Special sentinel year used in the toolbar combo to represent "كل السنوات".
    /// The repository treats <c>null</c> as the all-years filter; this wrapper
    /// is needed because <see cref="ExamYearOption"/> is non-nullable in the
    /// observable collection.
    /// </summary>
    public static readonly ExamYearOption AllYearsOption = new(0, "كل السنوات", false);

    public ExamsViewModel(
        IExamsRepository repo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors)
    {
        _repo = repo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;

        Years.Add(AllYearsOption);
        _selectedYear = AllYearsOption;

        _isInitializing = false;
    }

    public override string TitleAr => "أنواع الامتحانات";
    public override string SubtitleAr => $"إجمالي: {Items.Count:N0}";

    public ObservableCollection<ExamRow> Items { get; } = new();
    public ObservableCollection<ExamYearOption> Years { get; } = new();

    [ObservableProperty] private ExamYearOption _selectedYear;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ExamRow? _selectedRow;

    public bool HasResults => Items.Count > 0;
    public bool IsEmpty => !IsLoading && Items.Count == 0;

    partial void OnSelectedYearChanged(ExamYearOption value)
    {
        if (_isInitializing) return;
        _ = ReloadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_isInitializing) return;
        DebouncedReload();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Years.Count <= 1)
        {
            await LoadYearsAsync(ct).ConfigureAwait(true);

            // First-time only: snap the filter to the current academic year.
            try
            {
                var currentId = await _repo.GetCurrentAcademicYearIdAsync(ct).ConfigureAwait(true);
                if (currentId.HasValue)
                {
                    var match = Years.FirstOrDefault(y => y.Id == currentId.Value);
                    if (match is not null)
                    {
                        _isInitializing = true;
                        SelectedYear = match;
                        _isInitializing = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _errors.Report("تعذّر تحديد السنة الحالية", ex.Message, ex);
            }
        }

        await ReloadAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadYearsAsync(CancellationToken ct)
    {
        try
        {
            var years = await _repo.GetYearsAsync(ct).ConfigureAwait(true);
            _allYears.Clear();
            _allYears.AddRange(years);

            _isInitializing = true;
            Years.Clear();
            Years.Add(AllYearsOption);
            foreach (var y in years) Years.Add(y);
            SelectedYear = AllYearsOption;
            _isInitializing = false;
        }
        catch (Exception ex)
        {
            _isInitializing = false;
            StatusMessage = "تعذّر تحميل السنوات الدراسية.";
            _errors.Report("تعذّر تحميل السنوات الدراسية", ex.Message, ex);
        }
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight) return;
        _reloadInFlight = true;
        try
        {
            IsLoading = true;
            StatusMessage = null;

            int? yearId = SelectedYear is null || SelectedYear.Id == 0 ? null : SelectedYear.Id;
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;

            var rows = await _repo.GetAllAsync(yearId, search, ct).ConfigureAwait(true);

            Items.Clear();
            foreach (var r in rows) Items.Add(r);

            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SubtitleAr));
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل قائمة الامتحانات.";
            _errors.Report("تعذّر تحميل قائمة الامتحانات", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    private void DebouncedReload()
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, cts.Token).ConfigureAwait(false);
                if (cts.IsCancellationRequested) return;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    ReloadAsync(cts.Token));
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _isInitializing = true;
        SelectedYear = AllYearsOption;
        SearchText = string.Empty;
        _isInitializing = false;
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task AddExamAsync()
    {
        var years = _allYears.Count > 0 ? _allYears : new List<ExamYearOption>();
        if (years.Count == 0)
        {
            await _dialogs.ShowWarningAsync(
                "لا توجد سنوات",
                "لا توجد سنة دراسية مُعرَّفة. أضف سنة دراسية أولاً من الإعدادات.").ConfigureAwait(true);
            return;
        }

        var currentYearId = await _repo.GetCurrentAcademicYearIdAsync().ConfigureAwait(true);
        var defaultYearId = currentYearId
            ?? (SelectedYear is not null && SelectedYear.Id != 0 ? SelectedYear.Id : years[0].Id);

        var model = new ExamSaveModel
        {
            AcademicYearId = defaultYearId,
            Weight = 1m,
        };

        var ok = ExamEditorDialog.Show(model, isEdit: false, years);
        if (!ok) return;

        try
        {
            await _repo.CreateAsync(model).ConfigureAwait(true);
            _toasts.Success("تمت إضافة الامتحان", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّرت الإضافة", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّرت إضافة الامتحان", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task EditExamAsync(ExamRow? row)
    {
        if (row is null) return;
        var years = _allYears.Count > 0 ? _allYears : new List<ExamYearOption>();
        if (years.Count == 0) return;

        var model = new ExamSaveModel
        {
            Id = row.Id,
            NameAr = row.NameAr,
            AcademicYearId = row.AcademicYearId,
            Weight = row.Weight,
        };

        var ok = ExamEditorDialog.Show(model, isEdit: true, years);
        if (!ok) return;

        try
        {
            await _repo.UpdateAsync(model).ConfigureAwait(true);
            _toasts.Success("تم تعديل الامتحان", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر التعديل", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تعديل الامتحان", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteExamAsync(ExamRow? row)
    {
        if (row is null) return;

        if (row.MarksCount > 0)
        {
            await _dialogs.ShowWarningAsync(
                "لا يمكن الحذف",
                $"لا يمكن حذف الامتحان «{row.NameAr}» لوجود {row.MarksCount:N0} درجة مسجلة عليه. احذف الدرجات أولاً.")
                .ConfigureAwait(true);
            return;
        }

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف الامتحان",
            $"سيتم حذف الامتحان «{row.NameAr}» نهائياً. لا يمكن التراجع عن هذا الإجراء.",
            okText: "حذف نهائي").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.DeleteAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف الامتحان", row.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر الحذف", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف الامتحان", ex.Message, ex);
        }
    }
}
