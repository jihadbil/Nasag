using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Views.Common;
using Nasag.Views.Pages.Classes.Dialogs;
using Nasag.Views.Pages.Students;

namespace Nasag.ViewModels.Pages.Students;

public sealed partial class StudentsViewModel : PageViewModel
{
    private readonly IStudentsRepository _repo;
    private readonly IClassesRepository _classesRepo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly IUserPreferencesService _prefs;
    private readonly IExcelService _excel;
    private readonly StudentEditorViewModel _editor;
    private readonly StudentImportWizardViewModel _importWizard;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly ICurrentUserService _currentUser;

    private List<SectionOption> _allSections = new();
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// True while the constructor is wiring up fields — used by partial OnXxxChanged
    /// hooks to skip cascading <see cref="ReloadAsync"/> calls that would otherwise
    /// fire 2-3 times before the page is even shown (root cause of the "freeze on
    /// first navigation" bug).
    /// </summary>
    private bool _isInitializing = true;

    public StudentsViewModel(
        IStudentsRepository repo,
        IClassesRepository classesRepo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        IUserPreferencesService prefs,
        IExcelService excel,
        StudentEditorViewModel editor,
        StudentImportWizardViewModel importWizard,
        INavigationService navigation,
        IServiceProvider services,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _classesRepo = classesRepo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _prefs = prefs;
        _excel = excel;
        _editor = editor;
        _importWizard = importWizard;
        _navigation = navigation;
        _services = services;
        _currentUser = currentUser;
        _editor.Saved += OnEditorSaved;
        _editor.Cancelled += OnEditorCancelled;
        _currentUser.SignedIn += (_, _) => RefreshPermissions();
        _currentUser.SignedOut += (_, _) => RefreshPermissions();

        StatusOptions = new[]
        {
            new StudentStatusFilter(null, "جميع الحالات"),
            new StudentStatusFilter(StudentStatus.Active, "نشط"),
            new StudentStatusFilter(StudentStatus.Archived, "مؤرشف"),
            new StudentStatusFilter(StudentStatus.Graduated, "متخرّج"),
        };
        PageSizeOptions = new[] { 10, 20, 50, 100 };

        // Assign backing fields directly so the source-generated setters (and their
        // partial OnXxxChanged hooks) don't fire during construction.
        _selectedStatus = StatusOptions[0];
        _pageSize = prefs.Current.StudentsPageSize > 0 ? prefs.Current.StudentsPageSize : 20;
        _canManageFees = _currentUser.HasPermission(Permission.ManageFees);

        _isInitializing = false;
    }

    /// <summary>
    /// Visibility flag bound from <c>StudentsView.xaml</c> on the "open fees"
    /// row-action button. Refreshed on sign-in/out so role changes propagate.
    /// </summary>
    [ObservableProperty]
    private bool _canManageFees;

    private void RefreshPermissions()
    {
        CanManageFees = _currentUser.HasPermission(Permission.ManageFees);
    }

    public override string TitleAr => "الطلاب";
    public override string SubtitleAr => CurrentMode == StudentsPageMode.Editor
        ? "أدخل بيانات الطالب وولي الأمر ثم احفظ."
        : "إدارة بيانات الطلاب والبحث والفلترة";

    public StudentEditorViewModel Editor => _editor;

    public ObservableCollection<StudentRow> Students { get; } = new();
    public ObservableCollection<GradeOption> Grades { get; } = new();
    public ObservableCollection<SectionOption> AvailableSections { get; } = new();
    public ObservableCollection<int> PageNumbers { get; } = new();
    public IReadOnlyList<StudentStatusFilter> StatusOptions { get; }
    public IReadOnlyList<int> PageSizeOptions { get; }

    [ObservableProperty] private StudentsPageMode _currentMode = StudentsPageMode.List;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private GradeOption? _selectedGrade;
    [ObservableProperty] private SectionOption? _selectedSection;
    [ObservableProperty] private StudentStatusFilter _selectedStatus;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _pageSize;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _archivedCount;
    [ObservableProperty] private int _allCount;
    [ObservableProperty] private StudentRow? _selectedRow;

    public bool ShowList => CurrentMode == StudentsPageMode.List;
    public bool ShowEditor => CurrentMode == StudentsPageMode.Editor;
    public bool HasResults => Students.Count > 0;
    public bool IsEmpty => !IsLoading && Students.Count == 0;
    public bool CanGoNext => Page < TotalPages;
    public bool CanGoPrev => Page > 1;
    public bool CanActOnSelectedRow => SelectedRow is not null && CurrentMode == StudentsPageMode.List;

    public string PaginationLabel => TotalCount == 0
        ? "لا توجد نتائج"
        : $"الصفحة {Page} من {Math.Max(TotalPages, 1)} — إجمالي {TotalCount:N0}";

    partial void OnCurrentModeChanged(StudentsPageMode value)
    {
        OnPropertyChanged(nameof(ShowList));
        OnPropertyChanged(nameof(ShowEditor));
        OnPropertyChanged(nameof(SubtitleAr));
        OnPropertyChanged(nameof(CanActOnSelectedRow));
    }

    partial void OnPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(PaginationLabel));
        NextPageCommand.NotifyCanExecuteChanged();
        PrevPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(PaginationLabel));
        NextPageCommand.NotifyCanExecuteChanged();
        PrevPageCommand.NotifyCanExecuteChanged();
        RebuildPageNumbers();
    }

    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PaginationLabel));

    partial void OnSelectedRowChanged(StudentRow? value)
    {
        OnPropertyChanged(nameof(CanActOnSelectedRow));
        EditSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_isInitializing) return;
        DebouncedReload();
    }

    partial void OnSelectedStatusChanged(StudentStatusFilter value)
    {
        if (_isInitializing) return;
        ResetPageAndReload();
    }

    partial void OnPageSizeChanged(int value)
    {
        if (_isInitializing) return;
        _prefs.Current.StudentsPageSize = value;
        _prefs.Save();
        ResetPageAndReload();
    }

    partial void OnSelectedGradeChanged(GradeOption? value)
    {
        AvailableSections.Clear();
        if (value is not null)
        {
            foreach (var s in _allSections.Where(s => s.GradeId == value.Id))
                AvailableSections.Add(s);
        }
        if (SelectedSection is not null && SelectedSection.GradeId != value?.Id)
            SelectedSection = null;
        if (_isInitializing) return;
        ResetPageAndReload();
    }

    partial void OnSelectedSectionChanged(SectionOption? value)
    {
        if (_isInitializing) return;
        ResetPageAndReload();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Grades.Count == 0)
            await LoadLookupsAsync(ct).ConfigureAwait(true);
        await ReloadAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        try
        {
            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);
            Grades.Clear();
            foreach (var g in lookups.Grades) Grades.Add(g);
            _allSections = lookups.Sections.ToList();
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل الصفوف والشعب.";
            _errors.Report("تعذّر تحميل الصفوف والشعب", ex.Message, ex);
        }
    }

    private bool _reloadInFlight;

    /// <summary>
    /// Public entry point — guards against re-entrance so setter cascades during
    /// init/teardown don't race with an in-flight reload.
    /// </summary>
    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight) return;
        _reloadInFlight = true;
        try
        {
            await ReloadCoreAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    /// <summary>
    /// Inner reload — recursable. Used by ReloadAsync (under guard) and by the
    /// page-overflow correction path below (which needs to recurse without
    /// tripping the guard).
    /// </summary>
    private async Task ReloadCoreAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            StatusMessage = null;

            var stats = await _repo.GetStatsAsync(ct).ConfigureAwait(true);
            AllCount = stats.Total;
            ActiveCount = stats.Active;
            ArchivedCount = stats.Archived;

            var sort = _prefs.Current.StudentsSortAlphabetically
                ? StudentSortMode.Alphabetical
                : StudentSortMode.NewestFirst;

            var query = new StudentsQuery(
                Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                GradeId: SelectedGrade?.Id,
                SectionId: SelectedSection?.Id,
                Status: SelectedStatus?.Value,
                Page: Page,
                PageSize: PageSize,
                Sort: sort);

            var page = await _repo.SearchAsync(query, ct).ConfigureAwait(true);

            Students.Clear();
            foreach (var row in page.Items) Students.Add(row);
            TotalCount = page.TotalCount;
            TotalPages = page.TotalPages;
            if (Page > TotalPages && TotalPages > 0)
            {
                Page = TotalPages;
                await ReloadCoreAsync(ct).ConfigureAwait(true);
                return;
            }
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل قائمة الطلاب.";
            _errors.Report("تعذّر تحميل قائمة الطلاب", ex.Message, ex);
        }
    }

    private void RebuildPageNumbers()
    {
        PageNumbers.Clear();
        var count = Math.Max(TotalPages, 1);
        for (var i = 1; i <= count; i++) PageNumbers.Add(i);
    }

    private void ResetPageAndReload()
    {
        Page = 1;
        _ = ReloadAsync();
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
                {
                    Page = 1;
                    return ReloadAsync(cts.Token);
                });
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedGrade = null;
        SelectedSection = null;
        SelectedStatus = StatusOptions[0];
        SearchText = string.Empty;
        Page = 1;
        _ = ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        if (!CanGoNext) return;
        Page++;
        await ReloadAsync().ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrev))]
    private async Task PrevPageAsync()
    {
        if (!CanGoPrev) return;
        Page--;
        await ReloadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task JumpToPageAsync(int? target)
    {
        if (target is null) return;
        var clamped = Math.Max(1, Math.Min(TotalPages, target.Value));
        if (clamped == Page) return;
        Page = clamped;
        await ReloadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AddStudentAsync()
    {
        await _editor.LoadForCreateAsync().ConfigureAwait(true);
        CurrentMode = StudentsPageMode.Editor;
    }

    [RelayCommand]
    private async Task EditStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        await _editor.LoadForEditAsync(row.Id).ConfigureAwait(true);
        CurrentMode = StudentsPageMode.Editor;
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelectedRow))]
    private async Task EditSelectedAsync()
    {
        if (SelectedRow is null) return;
        await EditStudentAsync(SelectedRow).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ArchiveStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        var ok = await _dialogs.ConfirmAsync(
            "أرشفة الطالب",
            $"هل تريد أرشفة الطالب «{row.FullName}»؟ لن يظهر في القائمة الافتراضية.").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.SetStatusAsync(row.Id, StudentStatus.Archived).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
            _toasts.Success("تمت الأرشفة", row.FullName);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر أرشفة الطالب", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task RestoreStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        try
        {
            await _repo.SetStatusAsync(row.Id, StudentStatus.Active).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
            _toasts.Success("تمت الاستعادة", row.FullName);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر استعادة الطالب", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف الطالب نهائياً",
            $"سيتم حذف الطالب «{row.FullName}» وكل بياناته نهائياً. لا يمكن التراجع عن هذا الإجراء.",
            okText: "حذف نهائي").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _repo.DeleteAsync(row.Id).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
            _toasts.Success("تم حذف الطالب", row.FullName);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف الطالب", ex.Message, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelectedRow))]
    private async Task DeleteSelectedAsync() => await DeleteStudentAsync(SelectedRow).ConfigureAwait(true);

    [RelayCommand]
    private async Task MoveStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        try
        {
            var targets = await _classesRepo.GetMoveTargetsAsync(row.Id).ConfigureAwait(true);
            // Drop the student's current section from the list.
            var currentSectionLabel = $"{row.GradeName} — {row.SectionName}";
            var filtered = targets.Where(t => !(t.GradeName == row.GradeName && t.NameAr == row.SectionName)).ToList();
            if (filtered.Count == 0)
            {
                await _dialogs.ShowWarningAsync(
                    "لا توجد شعبة أخرى",
                    "لا توجد شعبة أخرى في السنة الحالية لنقل الطالب إليها.").ConfigureAwait(true);
                return;
            }

            var picked = MoveStudentDialog.Show(row.FullName, currentSectionLabel, filtered);
            if (picked is null) return;

            await _classesRepo.MoveStudentAsync(row.Id, picked.Id).ConfigureAwait(true);
            _toasts.Success("تم نقل الطالب", $"{row.FullName} → {picked.GradeName} - {picked.NameAr}");
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر النقل", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر نقل الطالب", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "تصدير الطلاب إلى Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"الطلاب-{DateTime.Today:yyyy-MM-dd}.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            var rows = await _repo.GetAllForExportAsync().ConfigureAwait(true);
            await _excel.ExportStudentsAsync(dlg.FileName, rows).ConfigureAwait(true);
            _toasts.Success("تم التصدير بنجاح", Path.GetFileName(dlg.FileName));
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تصدير الطلاب", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        try
        {
            await _importWizard.PrepareAsync().ConfigureAwait(true);
            var window = new StudentImportWizard
            {
                DataContext = _importWizard,
                Owner = System.Windows.Application.Current?.MainWindow
            };
            var result = window.ShowDialog();
            if (result == true)
            {
                await ReloadAsync().ConfigureAwait(true);
                _toasts.Success("تم الاستيراد",
                    $"تمت إضافة {_importWizard.InsertedCount} طالباً.");
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر استيراد الطلاب", ex.Message, ex);
        }
    }

    private async void OnEditorSaved(object? sender, EventArgs e)
    {
        CurrentMode = StudentsPageMode.List;
        await ReloadAsync().ConfigureAwait(true);
    }

    private void OnEditorCancelled(object? sender, EventArgs e)
        => CurrentMode = StudentsPageMode.List;

    [RelayCommand]
    private async Task OpenFeesForStudentAsync(StudentRow? row)
    {
        if (row is null) return;
        try
        {
            _navigation.NavigateTo(NavigationSection.Fees);
            var feesVm = _services.GetRequiredService<Nasag.ViewModels.Pages.Fees.FeesViewModel>();
            await feesVm.PrepareForStudentAsync(row.Id).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر فتح رسوم الطالب", ex.Message, ex);
        }
    }
}

public enum StudentsPageMode { List, Editor }

public sealed record StudentStatusFilter(StudentStatus? Value, string Label);
