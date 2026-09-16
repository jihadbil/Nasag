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
using Nasag.Views.Pages.Subjects.Dialogs;

namespace Nasag.ViewModels.Pages.Subjects;

public sealed partial class SubjectsViewModel : PageViewModel
{
    private readonly ISubjectsRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;

    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// True only while the constructor wires up backing fields. Partial
    /// OnXxxChanged hooks short-circuit during construction to avoid the
    /// well-known "reload fires twice on first navigation" race.
    /// </summary>
    private bool _isInitializing = true;
    private bool _reloadInFlight;

    public SubjectsViewModel(
        ISubjectsRepository repo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors)
    {
        _repo = repo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;

        // Id == 0 is the "all grades" sentinel — kept here so that the
        // SearchableComboBox (which silently drops null items) can still
        // render "كل الصفوف" as a selectable filter.
        Grades.Add(AllGradesSentinel);
        _selectedGrade = AllGradesSentinel;

        _isInitializing = false;
    }

    private static readonly SubjectGradeOption AllGradesSentinel = new(0, "كل الصفوف");

    public override string TitleAr => "المواد الدراسية";
    public override string SubtitleAr => $"إجمالي المواد: {Items.Count:N0}";

    public ObservableCollection<SubjectRow> Items { get; } = new();

    /// <summary>
    /// Grade filter options. First entry is always <see cref="AllGradesSentinel"/>
    /// (Id == 0) which acts as the "all grades" filter.
    /// </summary>
    public ObservableCollection<SubjectGradeOption> Grades { get; } = new();

    [ObservableProperty] private SubjectGradeOption? _selectedGrade;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private SubjectRow? _selectedItem;

    public string TotalCountText => $"إجمالي المواد: {Items.Count:N0}";

    partial void OnSelectedGradeChanged(SubjectGradeOption? value)
    {
        if (_isInitializing) return;
        _ = ReloadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_isInitializing) return;
        DebouncedReload();
    }

    partial void OnSelectedItemChanged(SubjectRow? value)
    {
        EditSubjectCommand.NotifyCanExecuteChanged();
        DeleteSubjectCommand.NotifyCanExecuteChanged();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Items.Count == 0)
            await ReloadAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight) return;
        _reloadInFlight = true;
        try
        {
            await LoadGradesAsync(ct).ConfigureAwait(true);
            await LoadItemsAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    private async Task LoadGradesAsync(CancellationToken ct)
    {
        try
        {
            var keepId = SelectedGrade?.Id;
            var grades = await _repo.GetGradesAsync(ct).ConfigureAwait(true);

            _isInitializing = true;
            Grades.Clear();
            Grades.Add(AllGradesSentinel);
            foreach (var g in grades) Grades.Add(g);

            SelectedGrade = keepId.HasValue && keepId.Value > 0
                ? Grades.FirstOrDefault(g => g.Id == keepId.Value) ?? AllGradesSentinel
                : AllGradesSentinel;
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل الصفوف.";
            _errors.Report("تعذّر تحميل الصفوف", ex.Message, ex);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async Task LoadItemsAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            StatusMessage = null;

            var effectiveGradeId = SelectedGrade is null || SelectedGrade.Id == 0
                ? (int?)null
                : SelectedGrade.Id;
            var rows = await _repo
                .GetAllAsync(effectiveGradeId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText, ct)
                .ConfigureAwait(true);

            Items.Clear();
            foreach (var row in rows) Items.Add(row);

            OnPropertyChanged(nameof(SubtitleAr));
            OnPropertyChanged(nameof(TotalCountText));
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل المواد.";
            _errors.Report("تعذّر تحميل المواد", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
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
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ReloadAsync(cts.Token));
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _isInitializing = true;
        SelectedGrade = AllGradesSentinel;
        SearchText = string.Empty;
        _isInitializing = false;
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task AddSubjectAsync()
    {
        try
        {
            var gradeOptions = (await _repo.GetGradesAsync().ConfigureAwait(true)).ToList();
            if (gradeOptions.Count == 0)
            {
                await _dialogs.ShowWarningAsync(
                    "لا توجد صفوف",
                    "أضف صفاً واحداً على الأقل من شاشة «الصفوف» قبل إنشاء المواد.").ConfigureAwait(true);
                return;
            }

            var defaultGradeId = SelectedGrade != null && SelectedGrade.Id > 0
                ? SelectedGrade.Id
                : gradeOptions[0].Id;
            var model = new SubjectSaveModel
            {
                MaxMark = 100m,
                PassMark = 50m,
                GradeId = defaultGradeId,
            };
            if (!SubjectEditorDialog.Show(model, isEdit: false, gradeOptions)) return;

            await _repo.CreateAsync(model).ConfigureAwait(true);
            _toasts.Success("تمت إضافة المادة", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تعذّرت إضافة المادة", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّرت إضافة المادة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task EditSubjectAsync(SubjectRow? row)
    {
        if (row is null) return;
        try
        {
            var gradeOptions = (await _repo.GetGradesAsync().ConfigureAwait(true)).ToList();
            var model = new SubjectSaveModel
            {
                Id = row.Id,
                NameAr = row.NameAr,
                GradeId = row.GradeId,
                MaxMark = row.MaxMark,
                PassMark = row.PassMark,
            };
            if (!SubjectEditorDialog.Show(model, isEdit: true, gradeOptions)) return;

            await _repo.UpdateAsync(model).ConfigureAwait(true);
            _toasts.Success("تم تعديل المادة", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تعذّر تعديل المادة", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تعديل المادة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteSubjectAsync(SubjectRow? row)
    {
        if (row is null) return;

        var details = row.MarksCount > 0
            ? $"المادة «{row.NameAr}» — {row.GradeName}\nيوجد {row.MarksCount:N0} درجة مسجّلة. لن يسمح النظام بالحذف."
            : $"سيتم حذف المادة «{row.NameAr}» من الصف «{row.GradeName}» نهائياً. لا يمكن التراجع.";

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف المادة",
            details,
            okText: "حذف نهائي").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.DeleteAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف المادة", row.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تعذّر الحذف", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف المادة", ex.Message, ex);
        }
    }
}
