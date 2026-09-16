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

namespace Nasag.ViewModels.Pages.Results;

public sealed partial class ResultsViewModel : PageViewModel
{
    private readonly IResultsRepository _repo;
    private readonly IResultsCalculator _calculator;
    private readonly IErrorReporter _errors;

    private readonly List<ResultsSectionOption> _allSections = new();
    private readonly List<StudentResultRowViewModel> _allRows = new();

    private bool _isInitializing = true;
    private bool _reloadInFlight;
    private bool _reloadPending;
    private CancellationTokenSource? _searchCts;

    public ResultsViewModel(
        IResultsRepository repo,
        IResultsCalculator calculator,
        IErrorReporter errors)
    {
        _repo = repo;
        _calculator = calculator;
        _errors = errors;
        _isInitializing = false;
    }

    public override string TitleAr => "نتائج الطلاب";

    public override string SubtitleAr => HasSelection
        ? $"{SelectedSection!.GradeName} - {SelectedSection.NameAr} • {SelectedYear!.NameAr}"
        : "اختر الصف والشعبة والسنة الدراسية";

    public ObservableCollection<ResultsGradeOption> Grades { get; } = new();
    public ObservableCollection<ResultsSectionOption> AvailableSections { get; } = new();
    public ObservableCollection<ResultsYearOption> Years { get; } = new();
    public ObservableCollection<StudentResultRowViewModel> Rows { get; } = new();

    [ObservableProperty] private ResultsGradeOption? _selectedGrade;
    [ObservableProperty] private ResultsSectionOption? _selectedSection;
    [ObservableProperty] private ResultsYearOption? _selectedYear;
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _passedCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private decimal _highestPercentage;
    [ObservableProperty] private decimal _lowestPercentage;
    [ObservableProperty] private decimal _averagePercentage;
    [ObservableProperty] private bool _hasGradedRows;

    public bool HasSelection => SelectedSection is not null && SelectedYear is not null;
    public bool HasRows => Rows.Count > 0;

    public string TotalCountText => TotalCount.ToString("N0");
    public string PassedCountText => PassedCount.ToString("N0");
    public string FailedCountText => FailedCount.ToString("N0");
    public string PendingCountText => PendingCount.ToString("N0");
    public string HighestPercentageText => HasGradedRows ? $"{HighestPercentage:N1}%" : "—";
    public string LowestPercentageText => HasGradedRows ? $"{LowestPercentage:N1}%" : "—";
    public string AverageText => HasGradedRows ? $"{AveragePercentage:N1}%" : "—";

    partial void OnSelectedGradeChanged(ResultsGradeOption? value)
    {
        RefilterSections(preserveSelection: true);
        OnPropertyChanged(nameof(SubtitleAr));
        if (!_isInitializing)
            _ = LoadResultsAsync();
    }

    partial void OnSelectedSectionChanged(ResultsSectionOption? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        if (!_isInitializing)
            _ = LoadResultsAsync();
    }

    partial void OnSelectedYearChanged(ResultsYearOption? value)
    {
        RefilterSections(preserveSelection: true);
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SubtitleAr));
        if (!_isInitializing)
            _ = LoadResultsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_isInitializing) return;
        DebouncedFilter();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Grades.Count == 0)
            await LoadLookupsAsync(ct).ConfigureAwait(true);
        await LoadResultsAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await LoadLookupsAsync(ct).ConfigureAwait(true);
        await LoadResultsAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _isInitializing = true;
        try
        {
            SearchText = string.Empty;
            SelectedGrade = Grades.FirstOrDefault();
            // Year stays as is (default = current); section refilters from grade.
        }
        finally
        {
            _isInitializing = false;
        }
        ApplyLocalFilter();
        _ = LoadResultsAsync();
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            StatusMessage = null;
            _isInitializing = true;

            var keepGradeId = SelectedGrade?.Id;
            var keepSectionId = SelectedSection?.Id;
            var keepYearId = SelectedYear?.Id;

            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);

            Grades.Clear();
            foreach (var g in lookups.Grades) Grades.Add(g);

            _allSections.Clear();
            _allSections.AddRange(lookups.Sections);

            Years.Clear();
            foreach (var y in lookups.Years) Years.Add(y);

            // Year first — section filter depends on it.
            if (keepYearId.HasValue)
                SelectedYear = Years.FirstOrDefault(y => y.Id == keepYearId.Value);

            if (SelectedYear is null)
            {
                var currentId = await _repo.GetCurrentAcademicYearIdAsync(ct).ConfigureAwait(true);
                SelectedYear = (currentId.HasValue
                        ? Years.FirstOrDefault(y => y.Id == currentId.Value)
                        : null)
                    ?? Years.FirstOrDefault(y => y.IsActive)
                    ?? Years.FirstOrDefault();
            }

            SelectedGrade = (keepGradeId.HasValue ? Grades.FirstOrDefault(g => g.Id == keepGradeId.Value) : null)
                ?? Grades.FirstOrDefault();

            RefilterSections(preserveSelection: false);
            SelectedSection = (keepSectionId.HasValue
                    ? AvailableSections.FirstOrDefault(s => s.Id == keepSectionId.Value)
                    : null)
                ?? AvailableSections.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل القوائم.";
            _errors.Report("تعذر تحميل قوائم النتائج", ex.Message, ex);
        }
        finally
        {
            _isInitializing = false;
            IsLoading = false;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SubtitleAr));
        }
    }

    private void RefilterSections(bool preserveSelection)
    {
        var prevId = SelectedSection?.Id;
        AvailableSections.Clear();

        if (SelectedGrade is not null && SelectedYear is not null)
        {
            foreach (var s in _allSections.Where(x =>
                x.GradeId == SelectedGrade.Id && x.AcademicYearId == SelectedYear.Id))
            {
                AvailableSections.Add(s);
            }
        }

        if (preserveSelection && prevId.HasValue)
        {
            var kept = AvailableSections.FirstOrDefault(s => s.Id == prevId.Value);
            SelectedSection = kept ?? AvailableSections.FirstOrDefault();
        }
        else
        {
            SelectedSection = AvailableSections.FirstOrDefault();
        }
    }

    private async Task LoadResultsAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight)
        {
            _reloadPending = true;
            return;
        }

        if (SelectedSection is null || SelectedYear is null)
        {
            _allRows.Clear();
            Rows.Clear();
            RecalculateStats();
            return;
        }

        _reloadInFlight = true;
        try
        {
            IsLoading = true;
            StatusMessage = null;

            do
            {
                _reloadPending = false;
                var requestedSection = SelectedSection;
                var requestedYear = SelectedYear;
                if (requestedSection is null || requestedYear is null)
                {
                    _allRows.Clear();
                    Rows.Clear();
                    RecalculateStats();
                    return;
                }

                var inputs = await _repo
                    .GetStudentInputsAsync(requestedSection.Id, requestedYear.Id, ct)
                    .ConfigureAwait(true);

                if (SelectedSection?.Id != requestedSection.Id
                    || SelectedYear?.Id != requestedYear.Id)
                {
                    _reloadPending = true;
                    continue;
                }

                _allRows.Clear();
                foreach (var input in inputs)
                {
                    var summary = _calculator.Compute(input);
                    _allRows.Add(new StudentResultRowViewModel(summary));
                }

                ApplyLocalFilter();
                RecalculateStats();
            } while (_reloadPending);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل النتائج.";
            _errors.Report("تعذر تحميل النتائج", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    private void DebouncedFilter()
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
                {
                    ApplyLocalFilter();
                });
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    private void ApplyLocalFilter()
    {
        Rows.Clear();
        var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

        IEnumerable<StudentResultRowViewModel> filtered = _allRows;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = _allRows.Where(r =>
                r.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || r.StudentNumber.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var row in filtered) Rows.Add(row);
        OnPropertyChanged(nameof(HasRows));
    }

    private void RecalculateStats()
    {
        TotalCount = _allRows.Count;
        PassedCount = _allRows.Count(r => r.IsPassed);
        FailedCount = _allRows.Count(r => r.Grade == ResultGrade.Failed);
        PendingCount = _allRows.Count(r => r.Grade == ResultGrade.Pending);

        // Exclude pending students from Highest/Lowest/Average — their percentages are partial.
        var graded = _allRows.Where(r => r.Grade != ResultGrade.Pending).ToList();
        HasGradedRows = graded.Count > 0;
        if (HasGradedRows)
        {
            HighestPercentage = graded.Max(r => r.Percentage);
            LowestPercentage = graded.Min(r => r.Percentage);
            AveragePercentage = graded.Average(r => r.Percentage);
        }
        else
        {
            HighestPercentage = 0m;
            LowestPercentage = 0m;
            AveragePercentage = 0m;
        }

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(TotalCountText));
        OnPropertyChanged(nameof(PassedCountText));
        OnPropertyChanged(nameof(FailedCountText));
        OnPropertyChanged(nameof(PendingCountText));
        OnPropertyChanged(nameof(HighestPercentageText));
        OnPropertyChanged(nameof(LowestPercentageText));
        OnPropertyChanged(nameof(AverageText));
    }

    partial void OnHasGradedRowsChanged(bool value)
    {
        OnPropertyChanged(nameof(HighestPercentageText));
        OnPropertyChanged(nameof(LowestPercentageText));
        OnPropertyChanged(nameof(AverageText));
    }
}

public sealed class StudentResultRowViewModel
{
    public StudentResultRowViewModel(StudentResultSummary summary)
    {
        StudentId = summary.StudentId;
        StudentNumber = summary.StudentNumber;
        FullName = summary.FullName;
        Total = summary.Total;
        MaxTotal = summary.MaxTotal;
        ExaminedMax = summary.ExaminedMax;
        Percentage = summary.Percentage;
        Grade = summary.Grade;
        IsPassed = summary.IsPassed;
    }

    public int StudentId { get; }
    public string StudentNumber { get; }
    public string FullName { get; }
    public decimal Total { get; }
    public decimal MaxTotal { get; }
    public decimal ExaminedMax { get; }
    public decimal Percentage { get; }
    public ResultGrade Grade { get; }
    public bool IsPassed { get; }

    // Show "Total / ExaminedMax" so partial-exam students see realistic numbers, not "75 / 600".
    public string TotalDisplay => ExaminedMax > 0m
        ? $"{Total:N1} / {ExaminedMax:N0}"
        : "—";

    // Tooltip exposes the full-course max for context.
    public string TotalTooltip => MaxTotal > 0m
        ? $"إجمالي الصف: {MaxTotal:N0}"
        : string.Empty;

    public string PercentageDisplay => Grade switch
    {
        ResultGrade.Pending when ExaminedMax == 0m => "—",
        ResultGrade.Pending => $"~{Percentage:N1}%",
        _ => $"{Percentage:N1}%"
    };

    public string GradeLabelAr => Grade switch
    {
        ResultGrade.Excellent => "ممتاز",
        ResultGrade.VeryGood => "جيد جداً",
        ResultGrade.Good => "جيد",
        ResultGrade.Acceptable => "مقبول",
        ResultGrade.Pending => "غير مكتمل",
        _ => "راسب"
    };

    public string StatusLabelAr => Grade switch
    {
        ResultGrade.Pending => "غير مكتمل",
        ResultGrade.Failed => "راسب",
        _ => "ناجح"
    };
}
