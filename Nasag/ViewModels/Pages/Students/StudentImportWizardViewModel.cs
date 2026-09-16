using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

namespace Nasag.ViewModels.Pages.Students;

public enum ImportMode
{
    /// <summary>Append imported rows to the existing students.</summary>
    Append,
    /// <summary>Delete all existing students first, then import.</summary>
    Replace,
}

public enum WizardStep
{
    PickFile = 0,
    Preview = 1,
    Confirm = 2,
    Result = 3,
}

public sealed partial class StudentImportRowVm : ObservableObject
{
    public int RowNumber { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string GradeName { get; init; } = string.Empty;
    public string SectionName { get; init; } = string.Empty;
    public string GuardianFullName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
}

public sealed partial class StudentImportWizardViewModel : ObservableObject
{
    private readonly IExcelService _excel;
    private readonly IStudentsRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IErrorReporter _errors;

    private List<StudentSaveModel> _validModels = new();
    private List<GradeOption> _grades = new();
    private List<SectionOption> _sections = new();

    public StudentImportWizardViewModel(
        IExcelService excel,
        IStudentsRepository repo,
        IDialogService dialogs,
        IErrorReporter errors)
    {
        _excel = excel;
        _repo = repo;
        _dialogs = dialogs;
        _errors = errors;
    }

    public ObservableCollection<StudentImportRowVm> Rows { get; } = new();

    [ObservableProperty] private WizardStep _step = WizardStep.PickFile;
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private ImportMode _mode = ImportMode.Append;
    [ObservableProperty] private int _totalRows;
    [ObservableProperty] private int _validRows;
    [ObservableProperty] private int _invalidRows;
    [ObservableProperty] private int _insertedCount;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _busyMessage;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _success;
    [ObservableProperty] private bool _confirmed;

    public bool IsStepPickFile => Step == WizardStep.PickFile;
    public bool IsStepPreview  => Step == WizardStep.Preview;
    public bool IsStepConfirm  => Step == WizardStep.Confirm;
    public bool IsStepResult   => Step == WizardStep.Result;

    public bool ModeIsAppend
    {
        get => Mode == ImportMode.Append;
        set { if (value) Mode = ImportMode.Append; }
    }

    public bool ModeIsReplace
    {
        get => Mode == ImportMode.Replace;
        set { if (value) Mode = ImportMode.Replace; }
    }

    partial void OnStepChanged(WizardStep value)
    {
        OnPropertyChanged(nameof(IsStepPickFile));
        OnPropertyChanged(nameof(IsStepPreview));
        OnPropertyChanged(nameof(IsStepConfirm));
        OnPropertyChanged(nameof(IsStepResult));
    }

    partial void OnModeChanged(ImportMode value)
    {
        OnPropertyChanged(nameof(ModeIsAppend));
        OnPropertyChanged(nameof(ModeIsReplace));
    }

    /// <summary>Resets state and preloads lookups. Called before opening the wizard window.</summary>
    public async Task PrepareAsync(CancellationToken ct = default)
    {
        Step = WizardStep.PickFile;
        FilePath = null;
        Rows.Clear();
        _validModels.Clear();
        TotalRows = ValidRows = InvalidRows = InsertedCount = 0;
        StatusMessage = null;
        Success = false;
        Confirmed = false;
        Mode = ImportMode.Append;

        var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);
        _grades = lookups.Grades.ToList();
        _sections = lookups.Sections.ToList();
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر ملف Excel للاستيراد",
            Filter = "Excel (*.xlsx)|*.xlsx",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog() != true) return;
        FilePath = dlg.FileName;

        try
        {
            IsBusy = true;
            BusyMessage = "جاري قراءة الملف…";
            var raw = await _excel.ReadStudentsAsync(dlg.FileName).ConfigureAwait(true);
            ValidateRows(raw);
            Step = WizardStep.Preview;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر قراءة الملف", ex.Message, ex);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "حفظ قالب الاستيراد",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "قالب-استيراد-الطلاب.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _excel.WriteTemplateAsync(dlg.FileName).ConfigureAwait(true);
            await _dialogs.ShowSuccessAsync("تم الحفظ", $"تم حفظ القالب في:\n{dlg.FileName}").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حفظ القالب", ex.Message, ex);
        }
    }

    [RelayCommand] private void GoToConfirm() => Step = WizardStep.Confirm;
    [RelayCommand] private void BackToPreview() => Step = WizardStep.Preview;
    [RelayCommand] private void BackToPickFile() => Step = WizardStep.PickFile;

    [RelayCommand]
    private async Task RunImportAsync()
    {
        if (_validModels.Count == 0) return;

        if (Mode == ImportMode.Replace)
        {
            var ok = await _dialogs.ConfirmDestructiveAsync(
                "حذف جميع الطلاب الحاليين",
                "سيتم حذف جميع بيانات الطلاب الحاليين قبل الاستيراد. لا يمكن التراجع. هل تريد المتابعة؟",
                okText: "نعم، احذف ثم استورد").ConfigureAwait(true);
            if (!ok) return;
        }

        try
        {
            IsBusy = true;
            BusyMessage = Mode == ImportMode.Replace
                ? "جاري حذف الطلاب الحاليين ثم الاستيراد…"
                : "جاري الاستيراد…";

            if (Mode == ImportMode.Replace)
                await _repo.DeleteAllStudentsAsync().ConfigureAwait(true);

            InsertedCount = await _repo.BulkInsertAsync(_validModels).ConfigureAwait(true);
            Success = true;
            StatusMessage = $"تمت إضافة {InsertedCount} طالباً بنجاح.";
            Step = WizardStep.Result;
            Confirmed = true;
        }
        catch (Exception ex)
        {
            Success = false;
            StatusMessage = ex.Message;
            _errors.Report("تعذّر تنفيذ الاستيراد", ex.Message, ex);
            Step = WizardStep.Result;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void ValidateRows(IReadOnlyList<StudentImportRow> raw)
    {
        Rows.Clear();
        _validModels.Clear();
        TotalRows = raw.Count;
        var valid = 0;
        var invalid = 0;

        foreach (var r in raw)
        {
            var (model, error) = TryBuildModel(r);
            Rows.Add(new StudentImportRowVm
            {
                RowNumber = r.RowNumber,
                FullName = r.FullName ?? string.Empty,
                StudentNumber = r.StudentNumber ?? string.Empty,
                Gender = r.Gender ?? string.Empty,
                GradeName = r.GradeName ?? string.Empty,
                SectionName = r.SectionName ?? string.Empty,
                GuardianFullName = r.GuardianFullName ?? string.Empty,
                ErrorMessage = error,
            });

            if (error is null && model is not null)
            {
                _validModels.Add(model);
                valid++;
            }
            else
            {
                invalid++;
            }
        }

        ValidRows = valid;
        InvalidRows = invalid;
    }

    private (StudentSaveModel? model, string? error) TryBuildModel(StudentImportRow r)
    {
        if (string.IsNullOrWhiteSpace(r.FullName)) return (null, "الاسم الكامل مطلوب.");
        if (string.IsNullOrWhiteSpace(r.StudentNumber)) return (null, "رقم الطالب مطلوب.");
        if (string.IsNullOrWhiteSpace(r.GuardianFullName)) return (null, "اسم ولي الأمر مطلوب.");

        var gender = ParseGender(r.Gender);
        if (gender is null) return (null, "الجنس غير صالح (ذكر/أنثى).");

        var birth = ParseDate(r.BirthDate);
        if (birth is null) return (null, "تاريخ الميلاد غير صالح.");

        var enroll = ParseDate(r.EnrollmentDate) ?? DateTime.Today;

        var grade = _grades.FirstOrDefault(g =>
            string.Equals(g.NameAr, r.GradeName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (grade is null) return (null, $"الصف «{r.GradeName}» غير موجود.");

        var section = _sections.FirstOrDefault(s =>
            s.GradeId == grade.Id &&
            string.Equals(s.NameAr, r.SectionName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (section is null) return (null, $"الشعبة «{r.SectionName}» غير موجودة ضمن {grade.NameAr}.");

        var relation = ParseRelation(r.GuardianRelation) ?? GuardianRelation.Father;

        return (new StudentSaveModel
        {
            StudentNumber = r.StudentNumber!.Trim(),
            FullName = r.FullName!.Trim(),
            Gender = gender.Value,
            BirthDate = birth.Value,
            NationalId = r.NationalId,
            Phone = r.Phone,
            EnrollmentDate = enroll,
            Address = r.Address,
            Notes = r.Notes,
            SectionId = section.Id,
            GuardianFullName = r.GuardianFullName!.Trim(),
            GuardianRelation = relation,
            GuardianPhone = r.GuardianPhone,
            GuardianAltPhone = r.GuardianAltPhone,
            GuardianEmail = r.GuardianEmail,
            GuardianNationalId = r.GuardianNationalId,
            GuardianOccupation = r.GuardianOccupation,
            GuardianAddress = r.GuardianAddress,
        }, null);
    }

    private static Gender? ParseGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        if (v is "ذكر" or "M" or "m" or "ذ") return Gender.Male;
        if (v is "أنثى" or "انثى" or "F" or "f" or "أ") return Gender.Female;
        return null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(v, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
        if (DateTime.TryParse(v, out d)) return d;
        return null;
    }

    private static GuardianRelation? ParseRelation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim() switch
        {
            "أب" or "اب" => GuardianRelation.Father,
            "أم" or "ام" => GuardianRelation.Mother,
            "أخ" or "اخ" => GuardianRelation.Brother,
            "أخت" or "اخت" => GuardianRelation.Sister,
            "عم" or "خال" or "عم/خال" => GuardianRelation.Uncle,
            "عمة" or "خالة" or "عمة/خالة" => GuardianRelation.Aunt,
            "جد" => GuardianRelation.Grandfather,
            "جدة" => GuardianRelation.Grandmother,
            _ => GuardianRelation.Other,
        };
    }
}
