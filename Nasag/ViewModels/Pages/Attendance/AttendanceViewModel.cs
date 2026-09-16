using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;

namespace Nasag.ViewModels.Pages.Attendance;

public sealed partial class AttendanceViewModel : PageViewModel
{
    private readonly IAttendanceRepository _repo;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly List<AttendanceSectionOption> _allSections = new();
    private bool _isInitializing = true;
    private bool _reloadInFlight;
    private bool _reloadPending;

    public AttendanceViewModel(
        IAttendanceRepository repo,
        IToastService toasts,
        IErrorReporter errors)
    {
        _repo = repo;
        _toasts = toasts;
        _errors = errors;
        _selectedDate = DateTime.Today;
        _isInitializing = false;
    }

    public override string TitleAr => "الحضور والغياب";

    public override string SubtitleAr => SelectedSection is null
        ? "تسجيل حضور الطلاب اليومي"
        : $"{SelectedSection.GradeName} - {SelectedSection.NameAr} • {SelectedDateText}";

    public ObservableCollection<AttendanceGradeOption> Grades { get; } = new();
    public ObservableCollection<AttendanceSectionOption> AvailableSections { get; } = new();
    public ObservableCollection<AttendanceRowViewModel> Rows { get; } = new();

    [ObservableProperty] private AttendanceGradeOption? _selectedGrade;
    [ObservableProperty] private AttendanceSectionOption? _selectedSection;
    [ObservableProperty] private DateTime? _selectedDate;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _presentCount;
    [ObservableProperty] private int _absentCount;
    [ObservableProperty] private int _lateCount;
    [ObservableProperty] private int _excusedCount;

    public bool HasRows => Rows.Count > 0;
    public bool HasSelection => SelectedSection is not null && SelectedDate is not null;
    public bool CanSave => HasSelection && HasRows && !IsLoading && !IsSaving;
    public bool CanMarkAllPresent => HasRows && !IsLoading && !IsSaving;
    public string TotalCountText => TotalCount.ToString("N0");
    public string PresentCountText => PresentCount.ToString("N0");
    public string AbsentCountText => AbsentCount.ToString("N0");
    public string LateCountText => LateCount.ToString("N0");
    public string ExcusedCountText => ExcusedCount.ToString("N0");
    public string SelectedDateText => (SelectedDate ?? DateTime.Today).ToString("yyyy/MM/dd");

    partial void OnSelectedGradeChanged(AttendanceGradeOption? value)
    {
        AvailableSections.Clear();
        if (value is not null)
        {
            foreach (var section in _allSections.Where(s => s.GradeId == value.Id))
                AvailableSections.Add(section);
        }

        SelectedSection = AvailableSections.FirstOrDefault();
        OnPropertyChanged(nameof(SubtitleAr));
        if (!_isInitializing)
            _ = ReloadSheetAsync();
    }

    partial void OnSelectedSectionChanged(AttendanceSectionOption? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        NotifyCommandStates();
        if (!_isInitializing)
            _ = ReloadSheetAsync();
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(SelectedDateText));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        NotifyCommandStates();
        if (!_isInitializing)
            _ = ReloadSheetAsync();
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
            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);

            Grades.Clear();
            foreach (var grade in lookups.Grades) Grades.Add(grade);

            _allSections.Clear();
            _allSections.AddRange(lookups.Sections);

            SelectedGrade = (keepGradeId.HasValue ? Grades.FirstOrDefault(g => g.Id == keepGradeId.Value) : null)
                ?? Grades.FirstOrDefault();

            AvailableSections.Clear();
            if (SelectedGrade is not null)
            {
                foreach (var section in _allSections.Where(s => s.GradeId == SelectedGrade.Id))
                    AvailableSections.Add(section);
            }

            SelectedSection = (keepSectionId.HasValue
                    ? AvailableSections.FirstOrDefault(s => s.Id == keepSectionId.Value)
                    : null)
                ?? AvailableSections.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل الصفوف والشعب.";
            _errors.Report("تعذر تحميل الصفوف والشعب", ex.Message, ex);
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

        if (SelectedSection is null || SelectedDate is null)
        {
            if (SelectedSection is null)
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
                var requestedDate = SelectedDate;

                if (requestedSection is null || requestedDate is null)
                {
                    ClearRows();
                    return;
                }

                var requestedDay = requestedDate.Value.Date;
                var sheet = await _repo
                    .GetAttendanceSheetAsync(requestedSection.Id, requestedDay, ct)
                    .ConfigureAwait(true);

                if (SelectedSection?.Id != requestedSection.Id || SelectedDate?.Date != requestedDay)
                {
                    _reloadPending = true;
                    continue;
                }

                ClearRows();
                foreach (var row in sheet.Rows)
                {
                    var vm = new AttendanceRowViewModel(row);
                    vm.PropertyChanged += OnRowChanged;
                    Rows.Add(vm);
                }

                HasUnsavedChanges = false;
                RecalculateCounts();
            } while (_reloadPending);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل سجل الحضور.";
            _errors.Report("تعذر تحميل سجل الحضور", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
            NotifyCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanMarkAllPresent))]
    private void MarkAllPresent()
    {
        foreach (var row in Rows)
            row.Status = AttendanceStatus.Present;
        HasUnsavedChanges = true;
        RecalculateCounts();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveDayAsync(CancellationToken ct = default)
    {
        if (SelectedSection is null || SelectedDate is null) return;

        try
        {
            IsSaving = true;
            var rows = Rows
                .Select(r => new AttendanceSaveRow(r.StudentId, r.Status, r.Notes))
                .ToList();

            await _repo.SaveAttendanceSheetAsync(SelectedSection.Id, SelectedDate.Value.Date, rows, ct)
                .ConfigureAwait(true);

            HasUnsavedChanges = false;
            _toasts.Success("تم حفظ الحضور", $"{Rows.Count:N0} طالب");
            await ReloadSheetAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر حفظ الحضور", ex.Message, ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AttendanceRowViewModel.Status) or nameof(AttendanceRowViewModel.Notes))
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
        PresentCount = Rows.Count(r => r.Status == AttendanceStatus.Present);
        AbsentCount = Rows.Count(r => r.Status == AttendanceStatus.Absent);
        LateCount = Rows.Count(r => r.Status == AttendanceStatus.Late);
        ExcusedCount = Rows.Count(r => r.Status == AttendanceStatus.Excused);

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(TotalCountText));
        OnPropertyChanged(nameof(PresentCountText));
        OnPropertyChanged(nameof(AbsentCountText));
        OnPropertyChanged(nameof(LateCountText));
        OnPropertyChanged(nameof(ExcusedCountText));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanMarkAllPresent));
        SaveDayCommand.NotifyCanExecuteChanged();
        MarkAllPresentCommand.NotifyCanExecuteChanged();
    }
}

public sealed partial class AttendanceRowViewModel : ObservableObject
{
    private string? _notes;

    public AttendanceRowViewModel(AttendanceStudentRow row)
    {
        StudentId = row.StudentId;
        StudentNumber = row.StudentNumber;
        FullName = row.FullName;
        ExistingRecordId = row.ExistingRecordId;
        _status = row.Status;
        _notes = row.Notes;
    }

    public int StudentId { get; }
    public string StudentNumber { get; }
    public string FullName { get; }
    public int? ExistingRecordId { get; }
    public string StatusGroupName => $"AttendanceStatus{StudentId}";

    [ObservableProperty] private AttendanceStatus _status;

    public string? Notes
    {
        get => _notes;
        set
        {
            var next = string.IsNullOrEmpty(value) || value.Length <= 300 ? value : value[..300];
            SetProperty(ref _notes, next);
        }
    }

    public bool IsPresent
    {
        get => Status == AttendanceStatus.Present;
        set { if (value) Status = AttendanceStatus.Present; }
    }

    public bool IsAbsent
    {
        get => Status == AttendanceStatus.Absent;
        set { if (value) Status = AttendanceStatus.Absent; }
    }

    public bool IsLate
    {
        get => Status == AttendanceStatus.Late;
        set { if (value) Status = AttendanceStatus.Late; }
    }

    public bool IsExcused
    {
        get => Status == AttendanceStatus.Excused;
        set { if (value) Status = AttendanceStatus.Excused; }
    }

    partial void OnStatusChanged(AttendanceStatus value)
    {
        OnPropertyChanged(nameof(IsPresent));
        OnPropertyChanged(nameof(IsAbsent));
        OnPropertyChanged(nameof(IsLate));
        OnPropertyChanged(nameof(IsExcused));
    }
}
