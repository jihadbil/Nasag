using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Views.Pages.Classes.Dialogs;

namespace Nasag.ViewModels.Pages.Classes;

public sealed partial class ClassesViewModel : ViewModels.Pages.PageViewModel
{
    private readonly IClassesRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;

    private bool _isInitializing = true;
    private bool _reloadInFlight;

    public ClassesViewModel(
        IClassesRepository repo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors)
    {
        _repo = repo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;

        LevelOptions = new[]
        {
            new GradeLevelOption(GradeLevel.Primary, "تعليم أساسي"),
            new GradeLevelOption(GradeLevel.Middle, "تعليم إعدادي"),
            new GradeLevelOption(GradeLevel.High, "تعليم ثانوي"),
        };

        _isInitializing = false;
    }

    public override string TitleAr => "الصفوف والشعب";

    public override string SubtitleAr =>
        $"إجمالي: {Stats.GradeCount} صفّ • {Stats.SectionCount} شعبة • {Stats.StudentCount:N0} طالب نشط";

    public ObservableCollection<GradeRow> Grades { get; } = new();
    public ObservableCollection<SectionRow> Sections { get; } = new();
    public ObservableCollection<SectionStudentRow> StudentsInSection { get; } = new();

    public IReadOnlyList<GradeLevelOption> LevelOptions { get; }

    [ObservableProperty] private ClassesStats _stats = new(0, 0, 0);
    [ObservableProperty] private GradeRow? _selectedGrade;
    [ObservableProperty] private SectionRow? _selectedSection;

    public bool HasGrades => Grades.Count > 0;
    public bool HasSections => Sections.Count > 0;
    public bool HasSelectedGrade => SelectedGrade is not null;
    public bool HasSelectedSection => SelectedSection is not null;
    public bool HasStudentsInSection => StudentsInSection.Count > 0;

    public string SectionsTitle => SelectedGrade is null
        ? "شعب الصف"
        : $"شعب {SelectedGrade.NameAr}";

    public string StudentsTitle => SelectedSection is null
        ? "طلاب الشعبة"
        : $"طلاب {SelectedSection.NameAr}";

    partial void OnStatsChanged(ClassesStats value)
        => OnPropertyChanged(nameof(SubtitleAr));

    partial void OnSelectedGradeChanged(GradeRow? value)
    {
        OnPropertyChanged(nameof(HasSelectedGrade));
        OnPropertyChanged(nameof(SectionsTitle));
        AddSectionCommand.NotifyCanExecuteChanged();
        if (_isInitializing) return;
        _ = ReloadSectionsAsync();
    }

    partial void OnSelectedSectionChanged(SectionRow? value)
    {
        OnPropertyChanged(nameof(HasSelectedSection));
        OnPropertyChanged(nameof(StudentsTitle));
        if (_isInitializing) return;
        _ = ReloadStudentsAsync();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        await ReloadAsync(ct).ConfigureAwait(true);
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

            Stats = await _repo.GetStatsAsync(ct).ConfigureAwait(true);

            var grades = await _repo.GetGradesAsync(ct).ConfigureAwait(true);
            var keepGradeId = SelectedGrade?.Id;
            Grades.Clear();
            foreach (var g in grades) Grades.Add(g);
            OnPropertyChanged(nameof(HasGrades));

            // Restore selection (or pick the first grade).
            var nextGrade = (keepGradeId.HasValue ? Grades.FirstOrDefault(g => g.Id == keepGradeId.Value) : null)
                            ?? Grades.FirstOrDefault();
            if (!ReferenceEquals(nextGrade, SelectedGrade))
                SelectedGrade = nextGrade;
            else
                await ReloadSectionsAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل الصفوف.";
            _errors.Report("تعذّر تحميل الصفوف", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    private async Task ReloadSectionsAsync(CancellationToken ct = default)
    {
        Sections.Clear();
        StudentsInSection.Clear();
        SelectedSection = null;
        OnPropertyChanged(nameof(HasSections));
        OnPropertyChanged(nameof(HasStudentsInSection));

        if (SelectedGrade is null) return;
        try
        {
            var sections = await _repo.GetSectionsForGradeAsync(SelectedGrade.Id, ct).ConfigureAwait(true);
            foreach (var s in sections) Sections.Add(s);
            OnPropertyChanged(nameof(HasSections));
            SelectedSection = Sections.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحميل الشعب", ex.Message, ex);
        }
    }

    private async Task ReloadStudentsAsync(CancellationToken ct = default)
    {
        StudentsInSection.Clear();
        if (SelectedSection is null)
        {
            OnPropertyChanged(nameof(HasStudentsInSection));
            return;
        }
        try
        {
            var rows = await _repo.GetStudentsForSectionAsync(SelectedSection.Id, ct).ConfigureAwait(true);
            foreach (var r in rows) StudentsInSection.Add(r);
            OnPropertyChanged(nameof(HasStudentsInSection));
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحميل طلاب الشعبة", ex.Message, ex);
        }
    }

    // ----- Grade commands ---------------------------------------------------

    [RelayCommand]
    private async Task AddGradeAsync()
    {
        var nextSort = Grades.Count == 0 ? 1 : Grades.Max(g => g.SortOrder) + 1;
        var model = new GradeSaveModel { SortOrder = nextSort };
        if (!GradeEditorDialog.Show(model, isEdit: false, LevelOptions)) return;

        try
        {
            var newId = await _repo.CreateGradeAsync(model).ConfigureAwait(true);
            _toasts.Success("تمت إضافة الصف", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
            var created = Grades.FirstOrDefault(g => g.Id == newId);
            if (created is not null) SelectedGrade = created;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إضافة الصف", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task EditGradeAsync(GradeRow? row)
    {
        if (row is null) return;
        var model = new GradeSaveModel
        {
            Id = row.Id,
            NameAr = row.NameAr,
            Level = row.Level,
            SortOrder = row.SortOrder,
        };
        if (!GradeEditorDialog.Show(model, isEdit: true, LevelOptions)) return;

        try
        {
            await _repo.UpdateGradeAsync(model).ConfigureAwait(true);
            _toasts.Success("تم تحديث الصف", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحديث الصف", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteGradeAsync(GradeRow? row)
    {
        if (row is null) return;
        try
        {
            var deps = await _repo.GetGradeDependencyCountsAsync(row.Id).ConfigureAwait(true);

            var ok1 = await _dialogs.ConfirmDestructiveAsync(
                "حذف الصف",
                $"هل تريد حذف الصف «{row.NameAr}» بشكل نهائي؟",
                okText: "متابعة").ConfigureAwait(true);
            if (!ok1) return;

            if (deps.SectionCount > 0 || deps.StudentCount > 0 || deps.SubjectCount > 0)
            {
                var details = $"سيتم حذف:\n" +
                              $"• {deps.SectionCount} شعبة\n" +
                              $"• {deps.StudentCount} طالب (مع كل درجاتهم، حضورهم، ورسومهم)\n" +
                              $"• {deps.SubjectCount} مادة دراسية\n\n" +
                              $"لا يمكن التراجع عن هذا الإجراء.";
                var ok2 = await _dialogs.ConfirmDestructiveAsync(
                    "تأكيد نهائي",
                    details,
                    okText: "حذف الكل نهائياً").ConfigureAwait(true);
                if (!ok2) return;
            }

            await _repo.DeleteGradeAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف الصف", row.NameAr);
            if (SelectedGrade?.Id == row.Id) SelectedGrade = null;
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف الصف", ex.Message, ex);
        }
    }

    // ----- Section commands -------------------------------------------------

    private bool CanAddSection() => SelectedGrade is not null;

    [RelayCommand(CanExecute = nameof(CanAddSection))]
    private async Task AddSectionAsync()
    {
        if (SelectedGrade is null) return;
        var model = new SectionSaveModel { GradeId = SelectedGrade.Id, Capacity = 30 };
        if (!SectionEditorDialog.Show(model, isEdit: false, SelectedGrade.NameAr)) return;

        try
        {
            var id = await _repo.CreateSectionAsync(model).ConfigureAwait(true);
            _toasts.Success("تمت إضافة الشعبة", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
            var created = Sections.FirstOrDefault(s => s.Id == id);
            if (created is not null) SelectedSection = created;
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر الحفظ", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إضافة الشعبة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task EditSectionAsync(SectionRow? row)
    {
        if (row is null) return;
        var model = new SectionSaveModel
        {
            Id = row.Id,
            GradeId = row.GradeId,
            NameAr = row.NameAr,
            Capacity = row.Capacity,
        };
        if (!SectionEditorDialog.Show(model, isEdit: true, row.GradeName)) return;

        try
        {
            await _repo.UpdateSectionAsync(model).ConfigureAwait(true);
            _toasts.Success("تم تحديث الشعبة", model.NameAr);
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر الحفظ", ex.Message).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحديث الشعبة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteSectionAsync(SectionRow? row)
    {
        if (row is null) return;
        try
        {
            var deps = await _repo.GetSectionDependencyCountsAsync(row.Id).ConfigureAwait(true);

            var ok1 = await _dialogs.ConfirmDestructiveAsync(
                "حذف الشعبة",
                $"هل تريد حذف الشعبة «{row.NameAr}» من صف «{row.GradeName}»؟",
                okText: "متابعة").ConfigureAwait(true);
            if (!ok1) return;

            if (deps.StudentCount > 0)
            {
                var details = $"الشعبة تحتوي {deps.StudentCount} طالباً.\n\n" +
                              "سيتم حذف الطلاب وكل بياناتهم (الحضور، الدرجات، الرسوم، الأقساط، المدفوعات). " +
                              "لا يمكن التراجع عن هذا الإجراء.";
                var ok2 = await _dialogs.ConfirmDestructiveAsync(
                    "تأكيد نهائي",
                    details,
                    okText: "حذف الكل نهائياً").ConfigureAwait(true);
                if (!ok2) return;
            }

            await _repo.DeleteSectionAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف الشعبة", row.NameAr);
            if (SelectedSection?.Id == row.Id) SelectedSection = null;
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف الشعبة", ex.Message, ex);
        }
    }

    // ----- Student move -----------------------------------------------------

    [RelayCommand]
    private async Task MoveStudentAsync(SectionStudentRow? row)
    {
        if (row is null) return;
        try
        {
            var targets = await _repo.GetMoveTargetsAsync(row.Id).ConfigureAwait(true);
            // Exclude the current section.
            var filtered = targets.Where(t => t.Id != (SelectedSection?.Id ?? -1)).ToList();
            if (filtered.Count == 0)
            {
                await _dialogs.ShowWarningAsync(
                    "لا توجد شعبة أخرى",
                    "لا يوجد سوى شعبة واحدة في السنة الحالية. أضف شعبة جديدة قبل النقل.").ConfigureAwait(true);
                return;
            }

            var picked = MoveStudentDialog.Show(row.FullName, SelectedSection?.NameAr, filtered);
            if (picked is null) return;

            await _repo.MoveStudentAsync(row.Id, picked.Id).ConfigureAwait(true);
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

    /// <summary>
    /// Allows the StudentsView (or any caller) to trigger a move dialog for a
    /// student by id. Returns true if a move actually happened.
    /// </summary>
    public async Task<bool> MoveStudentByIdAsync(int studentId, string studentFullName, string? currentSectionName)
    {
        try
        {
            var targets = await _repo.GetMoveTargetsAsync(studentId).ConfigureAwait(true);
            if (targets.Count == 0)
            {
                await _dialogs.ShowWarningAsync(
                    "لا توجد شعب",
                    "لا توجد شعب في السنة الحالية لنقل الطالب إليها.").ConfigureAwait(true);
                return false;
            }
            var picked = MoveStudentDialog.Show(studentFullName, currentSectionName, targets);
            if (picked is null) return false;

            await _repo.MoveStudentAsync(studentId, picked.Id).ConfigureAwait(true);
            _toasts.Success("تم نقل الطالب", $"{studentFullName} → {picked.GradeName} - {picked.NameAr}");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            await _dialogs.ShowWarningAsync("تعذّر النقل", ex.Message).ConfigureAwait(true);
            return false;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر نقل الطالب", ex.Message, ex);
            return false;
        }
    }
}

public sealed record GradeLevelOption(GradeLevel Value, string Label)
{
    public override string ToString() => Label;
}
