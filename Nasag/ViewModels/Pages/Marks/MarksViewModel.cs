using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Repositories;
using Nasag.Services;

namespace Nasag.ViewModels.Pages.Marks;

public sealed partial class MarksViewModel : PageViewModel
{
    private readonly IMarksRepository _repo;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly List<MarksSectionOption> _allSections = new();
    private readonly List<MarksSubjectOption> _allSubjects = new();
    private bool _isInitializing = true;
    private bool _reloadInFlight;
    private bool _reloadPending;

    public MarksViewModel(
        IMarksRepository repo,
        IToastService toasts,
        IErrorReporter errors)
    {
        _repo = repo;
        _toasts = toasts;
        _errors = errors;
        _isInitializing = false;
    }

    public override string TitleAr => "إدخال الدرجات";

    public override string SubtitleAr => HasSelection
        ? $"{SelectedGrade!.NameAr} - {SelectedSection!.NameAr} • {SelectedSubject!.NameAr} • {SelectedExam!.NameAr}"
        : "اختر الصف والشعبة والمادة والامتحان لبدء الإدخال";

    public ObservableCollection<MarksGradeOption> Grades { get; } = new();
    public ObservableCollection<MarksSectionOption> AvailableSections { get; } = new();
    public ObservableCollection<MarksSubjectOption> AvailableSubjects { get; } = new();
    public ObservableCollection<MarksExamOption> Exams { get; } = new();
    public ObservableCollection<MarkRowViewModel> Rows { get; } = new();

    [ObservableProperty] private MarksGradeOption? _selectedGrade;
    [ObservableProperty] private MarksSectionOption? _selectedSection;
    [ObservableProperty] private MarksSubjectOption? _selectedSubject;
    [ObservableProperty] private MarksExamOption? _selectedExam;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _enteredCount;
    [ObservableProperty] private int _passedCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private int _missingCount;
    [ObservableProperty] private decimal _maxMark;
    [ObservableProperty] private decimal _passMark;
    [ObservableProperty] private decimal? _averageValue;

    public bool HasRows => Rows.Count > 0;
    public bool HasSelection => SelectedSection is not null && SelectedSubject is not null && SelectedExam is not null;
    public bool CanSave => HasSelection && HasRows && !IsLoading && !IsSaving;
    public bool CanClearAll => HasRows && !IsLoading && !IsSaving;
    public string TotalCountText => TotalCount.ToString("N0");
    public string EnteredCountText => EnteredCount.ToString("N0");
    public string PassedCountText => PassedCount.ToString("N0");
    public string FailedCountText => FailedCount.ToString("N0");
    public string MissingCountText => MissingCount.ToString("N0");
    public string MaxMarkText => MaxMark.ToString("0.##");
    public string PassMarkText => PassMark.ToString("0.##");
    public string AverageValueText => AverageValue.HasValue ? AverageValue.Value.ToString("N2") : "—";

    partial void OnSelectedGradeChanged(MarksGradeOption? value)
    {
        AvailableSections.Clear();
        AvailableSubjects.Clear();
        if (value is not null)
        {
            foreach (var section in _allSections.Where(s => s.GradeId == value.Id))
                AvailableSections.Add(section);
            foreach (var subject in _allSubjects.Where(s => s.GradeId == value.Id))
                AvailableSubjects.Add(subject);
        }

        SelectedSection = AvailableSections.FirstOrDefault();
        SelectedSubject = AvailableSubjects.FirstOrDefault();
        OnPropertyChanged(nameof(SubtitleAr));
        if (!_isInitializing)
            _ = ReloadSheetAsync();
    }

    partial void OnSelectedSectionChanged(MarksSectionOption? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        NotifyCommandStates();
        if (!_isInitializing)
        {
            WarnIfUnsavedChanges();
            _ = ReloadSheetAsync();
        }
    }

    partial void OnSelectedSubjectChanged(MarksSubjectOption? value)
    {
        MaxMark = value?.MaxMark ?? 0m;
        PassMark = value?.PassMark ?? 0m;
        OnPropertyChanged(nameof(MaxMarkText));
        OnPropertyChanged(nameof(PassMarkText));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        NotifyCommandStates();
        if (!_isInitializing)
        {
            WarnIfUnsavedChanges();
            _ = ReloadSheetAsync();
        }
    }

    partial void OnSelectedExamChanged(MarksExamOption? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        NotifyCommandStates();
        if (!_isInitializing)
        {
            WarnIfUnsavedChanges();
            _ = ReloadSheetAsync();
        }
    }

    private void WarnIfUnsavedChanges()
    {
        if (HasUnsavedChanges)
        {
            _toasts.Warning("تغييرات غير محفوظة", "تم تجاهل التعديلات غير المحفوظة");
            HasUnsavedChanges = false;
        }
    }

    partial void OnIsSavingChanged(bool value) => NotifyCommandStates();

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Grades.Count == 0)
            await LoadLookupsAsync(ct).ConfigureAwait(true);
        await ReloadSheetAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await LoadLookupsAsync(ct).ConfigureAwait(true);
        await ReloadSheetAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            NotifyCommandStates();
            StatusMessage = null;
            _isInitializing = true;

            var keepGradeId = SelectedGrade?.Id;
            var keepSectionId = SelectedSection?.Id;
            var keepSubjectId = SelectedSubject?.Id;
            var keepExamId = SelectedExam?.Id;

            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);

            Grades.Clear();
            foreach (var g in lookups.Grades) Grades.Add(g);

            _allSections.Clear();
            _allSections.AddRange(lookups.Sections);

            _allSubjects.Clear();
            _allSubjects.AddRange(lookups.Subjects);

            Exams.Clear();
            foreach (var e in lookups.Exams) Exams.Add(e);

            SelectedGrade = (keepGradeId.HasValue ? Grades.FirstOrDefault(g => g.Id == keepGradeId.Value) : null)
                ?? Grades.FirstOrDefault();

            AvailableSections.Clear();
            AvailableSubjects.Clear();
            if (SelectedGrade is not null)
            {
                foreach (var s in _allSections.Where(s => s.GradeId == SelectedGrade.Id))
                    AvailableSections.Add(s);
                foreach (var s in _allSubjects.Where(s => s.GradeId == SelectedGrade.Id))
                    AvailableSubjects.Add(s);
            }

            SelectedSection = (keepSectionId.HasValue
                    ? AvailableSections.FirstOrDefault(s => s.Id == keepSectionId.Value)
                    : null)
                ?? AvailableSections.FirstOrDefault();

            SelectedSubject = (keepSubjectId.HasValue
                    ? AvailableSubjects.FirstOrDefault(s => s.Id == keepSubjectId.Value)
                    : null)
                ?? AvailableSubjects.FirstOrDefault();

            SelectedExam = (keepExamId.HasValue
                    ? Exams.FirstOrDefault(e => e.Id == keepExamId.Value)
                    : null)
                ?? Exams.FirstOrDefault();

            MaxMark = SelectedSubject?.MaxMark ?? 0m;
            PassMark = SelectedSubject?.PassMark ?? 0m;
            OnPropertyChanged(nameof(MaxMarkText));
            OnPropertyChanged(nameof(PassMarkText));
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل القوائم.";
            _errors.Report("تعذر تحميل قوائم الدرجات", ex.Message, ex);
        }
        finally
        {
            _isInitializing = false;
            IsLoading = false;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SubtitleAr));
            NotifyCommandStates();
        }
    }

    private async Task ReloadSheetAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight)
        {
            _reloadPending = true;
            return;
        }

        if (!HasSelection)
        {
            ClearRows();
            return;
        }

        _reloadInFlight = true;
        try
        {
            IsLoading = true;
            NotifyCommandStates();
            StatusMessage = null;

            do
            {
                _reloadPending = false;
                var requestedSection = SelectedSection;
                var requestedSubject = SelectedSubject;
                var requestedExam = SelectedExam;

                if (requestedSection is null || requestedSubject is null || requestedExam is null)
                {
                    ClearRows();
                    return;
                }

                var sheet = await _repo
                    .GetSheetAsync(requestedSection.Id, requestedSubject.Id, requestedExam.Id, ct)
                    .ConfigureAwait(true);

                if (SelectedSection?.Id != requestedSection.Id
                    || SelectedSubject?.Id != requestedSubject.Id
                    || SelectedExam?.Id != requestedExam.Id)
                {
                    _reloadPending = true;
                    continue;
                }

                MaxMark = sheet.MaxMark;
                PassMark = sheet.PassMark;
                OnPropertyChanged(nameof(MaxMarkText));
                OnPropertyChanged(nameof(PassMarkText));

                ClearRows();
                foreach (var row in sheet.Rows)
                {
                    var vm = new MarkRowViewModel(row, sheet.MaxMark, sheet.PassMark);
                    vm.PropertyChanged += OnRowChanged;
                    Rows.Add(vm);
                }

                HasUnsavedChanges = false;
                RecalculateCounts();
            } while (_reloadPending);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل قائمة الدرجات.";
            _errors.Report("تعذر تحميل قائمة الدرجات", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
            NotifyCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private void ClearAll()
    {
        foreach (var row in Rows)
        {
            row.Value = null;
            row.Notes = null;
        }
        HasUnsavedChanges = true;
        RecalculateCounts();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (SelectedSection is null || SelectedSubject is null || SelectedExam is null) return;

        try
        {
            IsSaving = true;
            var rows = Rows
                .Select(r => new MarkSaveRow(r.StudentId, r.Value, r.Notes))
                .ToList();

            await _repo.SaveSheetAsync(SelectedSection.Id, SelectedSubject.Id, SelectedExam.Id, rows, ct)
                .ConfigureAwait(true);

            HasUnsavedChanges = false;
            _toasts.Success("تم حفظ الدرجات", $"{EnteredCount:N0} درجة مُدخَلة");
            await ReloadSheetAsync(ct).ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تحقق الإدخال", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر حفظ الدرجات", ex.Message, ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarkRowViewModel.Value) or nameof(MarkRowViewModel.Notes))
        {
            HasUnsavedChanges = true;
            RecalculateCounts();
        }
    }

    private void ClearRows()
    {
        foreach (var row in Rows)
            row.PropertyChanged -= OnRowChanged;
        Rows.Clear();
        RecalculateCounts();
    }

    private void RecalculateCounts()
    {
        TotalCount = Rows.Count;
        var entered = Rows.Where(r => r.Value.HasValue).ToList();
        EnteredCount = entered.Count;
        MissingCount = Rows.Count - EnteredCount;
        PassedCount = entered.Count(r => r.Value!.Value >= PassMark);
        FailedCount = entered.Count(r => r.Value!.Value < PassMark);
        AverageValue = entered.Count == 0 ? null : entered.Average(r => r.Value!.Value);

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(TotalCountText));
        OnPropertyChanged(nameof(EnteredCountText));
        OnPropertyChanged(nameof(PassedCountText));
        OnPropertyChanged(nameof(FailedCountText));
        OnPropertyChanged(nameof(MissingCountText));
        OnPropertyChanged(nameof(AverageValueText));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanClearAll));
        SaveCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }
}

public sealed partial class MarkRowViewModel : ObservableObject
{
    private string? _notes;

    public MarkRowViewModel(MarksStudentRow row, decimal maxMark, decimal passMark)
    {
        StudentId = row.StudentId;
        StudentNumber = row.StudentNumber;
        FullName = row.FullName;
        ExistingMarkId = row.ExistingMarkId;
        MaxMark = maxMark;
        PassMark = passMark;
        _value = row.Value;
        _notes = row.Notes;
    }

    public int StudentId { get; }
    public string StudentNumber { get; }
    public string FullName { get; }
    public int? ExistingMarkId { get; }
    public decimal MaxMark { get; }
    public decimal PassMark { get; }

    [ObservableProperty] private decimal? _value;

    public string? Notes
    {
        get => _notes;
        set
        {
            var next = string.IsNullOrEmpty(value) || value.Length <= 300 ? value : value[..300];
            SetProperty(ref _notes, next);
        }
    }

    public bool IsAbsent => !Value.HasValue;
    public bool IsPassed => Value.HasValue && Value.Value >= PassMark;
    public bool IsFailed => Value.HasValue && Value.Value < PassMark;

    public string StatusLabel => IsAbsent ? "غير ممتحن" : (IsPassed ? "ناجح" : "راسب");

    partial void OnValueChanged(decimal? value)
    {
        OnPropertyChanged(nameof(IsAbsent));
        OnPropertyChanged(nameof(IsPassed));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(StatusLabel));
    }
}
