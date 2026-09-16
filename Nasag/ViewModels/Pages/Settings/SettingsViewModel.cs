using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Nasag.Licensing.License;
using Nasag.Models;
using Nasag.Services;
using Nasag.Services.Licensing;
using Nasag.ViewModels.Licensing;
using Nasag.ViewModels.Setup;
using Nasag.Views.Licensing;
using Nasag.Views.Pages.Settings;
using Nasag.Views.Setup;

namespace Nasag.ViewModels.Pages.Settings;

/// <summary>
/// View-model for the "School Settings" page (Phase 12). Owns:
///   • the singleton <see cref="SchoolSettings"/> row (school identity + logo + current year),
///   • the editable list of <see cref="AcademicYear"/> rows,
///   • the per-user prefs proxy for student-sort.
/// </summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly ISettingsRepository _repo;
    private readonly IFileService _files;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly IUserPreferencesService _prefs;
    private readonly IBusyService _busy;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _services;
    private readonly IApplicationRestarter _restarter;
    private readonly ILicenseService _license;
    private readonly IUpdateService _updates;

    private bool _isInitializing = true;
    private bool _reloadInFlight;

    public SettingsViewModel(
        ISettingsRepository repo,
        IFileService files,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        IUserPreferencesService prefs,
        IBusyService busy,
        ICurrentUserService currentUser,
        IServiceProvider services,
        IApplicationRestarter restarter,
        ILicenseService license,
        IUpdateService updates)
    {
        _repo = repo;
        _files = files;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _prefs = prefs;
        _busy = busy;
        _currentUser = currentUser;
        _services = services;
        _restarter = restarter;
        _license = license;
        _updates = updates;

        // Assign backing fields directly so OnXxxChanged partial methods do NOT fire
        // while the constructor runs (AI_INSTRUCTIONS section 13).
        _studentsSortAlphabetically = _prefs.Current.StudentsSortAlphabetically;
        _canManageSettings = _currentUser.HasPermission(Permission.ManageSettings);

        _license.StatusChanged += (_, _) => RefreshLicenseInfo();
        RefreshLicenseInfo();
        RefreshUpdateInfo();

        _isInitializing = false;
    }

    public override string TitleAr => "الإعدادات";
    public override string SubtitleAr => "بيانات المدرسة والسنة الدراسية والتفضيلات";

    // ─── School identity ─────────────────────────────────────────────────────
    [ObservableProperty] private string _nameAr = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _website;
    [ObservableProperty] private string? _principalName;
    [ObservableProperty] private byte[]? _logoBytes;

    public bool HasLogo => LogoBytes is { Length: > 0 };
    partial void OnLogoBytesChanged(byte[]? value) => OnPropertyChanged(nameof(HasLogo));

    // ─── Academic years ──────────────────────────────────────────────────────
    public ObservableCollection<AcademicYear> AcademicYears { get; } = new();
    [ObservableProperty] private AcademicYear? _selectedAcademicYear;

    // ─── Preferences ─────────────────────────────────────────────────────────
    [ObservableProperty] private bool _studentsSortAlphabetically;

    partial void OnStudentsSortAlphabeticallyChanged(bool value)
    {
        if (_isInitializing) return;
        _prefs.Current.StudentsSortAlphabetically = value;
        try
        {
            _prefs.Save();
            _toasts.Success("تم حفظ الإعداد",
                value ? "سيتم ترتيب الطلاب أبجدياً." : "سيظهر الطالب الأحدث في الأعلى.");
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حفظ الإعداد", ex.Message, ex);
        }
    }

    // ─── Permission gate (shown/hidden in XAML via BoolToVisibility) ─────────
    [ObservableProperty] private bool _canManageSettings;

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
            await _busy.RunAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    var settings = await _repo.GetOrCreateAsync(ct).ConfigureAwait(true);
                    var years = await _repo.GetAcademicYearsAsync(ct).ConfigureAwait(true);

                    // Suspend cascades while pumping the form fields.
                    _isInitializing = true;
                    try
                    {
                        NameAr = settings.NameAr ?? string.Empty;
                        Address = settings.Address;
                        Phone = settings.Phone;
                        Email = settings.Email;
                        Website = settings.Website;
                        PrincipalName = settings.PrincipalName;
                        LogoBytes = settings.LogoBytes;

                        AcademicYears.Clear();
                        foreach (var y in years) AcademicYears.Add(y);

                        SelectedAcademicYear = settings.CurrentAcademicYearId.HasValue
                            ? AcademicYears.FirstOrDefault(y => y.Id == settings.CurrentAcademicYearId.Value)
                            : AcademicYears.FirstOrDefault();
                    }
                    finally
                    {
                        _isInitializing = false;
                    }

                    StatusMessage = null;
                }
                finally
                {
                    IsLoading = false;
                }
            }, "جاري تحميل إعدادات المدرسة…").ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذّر تحميل الإعدادات.";
            _errors.Report("تعذّر تحميل الإعدادات", ex.Message, ex);
        }
        finally
        {
            _reloadInFlight = false;
        }
    }

    [RelayCommand]
    private async Task PickLogoAsync()
    {
        try
        {
            var picked = _files.PickImage();
            if (string.IsNullOrEmpty(picked)) return;
            var bytes = await _files.ReadAllBytesAsync(picked).ConfigureAwait(true);
            if (bytes is null) return;
            if (!_files.CanDisplayImage(bytes))
            {
                _toasts.Warning("تعذّر عرض الصورة", "اختر صورة بصيغة مدعومة (PNG / JPG / JPEG / BMP).");
                return;
            }
            LogoBytes = bytes;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحميل شعار المدرسة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void RemoveLogo() => LogoBytes = null;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        if (!CanManageSettings)
        {
            _toasts.Warning("لا توجد صلاحية", "ليس لديك صلاحية تعديل إعدادات المدرسة.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameAr))
        {
            _toasts.Warning("اسم المدرسة مطلوب", "يرجى إدخال اسم المدرسة قبل الحفظ.");
            return;
        }

        try
        {
            await _busy.RunAsync(async () =>
            {
                var payload = new SchoolSettings
                {
                    NameAr = NameAr,
                    Address = Address,
                    Phone = Phone,
                    Email = Email,
                    Website = Website,
                    PrincipalName = PrincipalName,
                    LogoBytes = LogoBytes,
                    CurrentAcademicYearId = SelectedAcademicYear?.Id
                };

                await _repo.SaveAsync(payload, ct).ConfigureAwait(true);
            }, "جاري حفظ إعدادات المدرسة…").ConfigureAwait(true);

            _toasts.Success("تم حفظ الإعدادات", "تم تحديث بيانات المدرسة.");
            await ReloadAsync(ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException invEx)
        {
            _toasts.Warning("تعذّر الحفظ", invEx.Message);
        }
        catch (UnauthorizedAccessException unEx)
        {
            _toasts.Warning("لا توجد صلاحية", unEx.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حفظ الإعدادات", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task CancelAsync(CancellationToken ct)
    {
        // "Cancel" reloads from DB to discard in-memory edits.
        await ReloadAsync(ct).ConfigureAwait(true);
        _toasts.Info("تم التراجع", "أُعيد تحميل الإعدادات الأصلية.");
    }

    [RelayCommand]
    private async Task AddAcademicYearAsync(CancellationToken ct)
    {
        if (!CanManageSettings)
        {
            _toasts.Warning("لا توجد صلاحية", "ليس لديك صلاحية تعديل إعدادات المدرسة.");
            return;
        }

        var result = AcademicYearDialog.ShowCreate();
        if (result is null) return;

        try
        {
            await _busy.RunAsync(async () =>
            {
                await _repo.CreateAcademicYearAsync(result.NameAr, result.StartDate, result.EndDate, ct)
                    .ConfigureAwait(true);
            }, "جاري إضافة السنة الدراسية…").ConfigureAwait(true);

            _toasts.Success("تمت الإضافة", $"تم إضافة السنة الدراسية «{result.NameAr}».");
            await ReloadAsync(ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException invEx)
        {
            _toasts.Warning("تعذّرت الإضافة", invEx.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّرت إضافة السنة الدراسية", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task EditAcademicYearAsync(AcademicYear? year)
    {
        if (year is null) return;
        if (!CanManageSettings)
        {
            _toasts.Warning("لا توجد صلاحية", "ليس لديك صلاحية تعديل إعدادات المدرسة.");
            return;
        }

        var result = AcademicYearDialog.ShowEdit(year.Id, year.NameAr, year.StartDate, year.EndDate);
        if (result is null) return;

        try
        {
            await _busy.RunAsync(async () =>
            {
                await _repo.UpdateAcademicYearAsync(year.Id, result.NameAr, result.StartDate, result.EndDate, CancellationToken.None)
                    .ConfigureAwait(true);
            }, "جاري تحديث السنة الدراسية…").ConfigureAwait(true);

            _toasts.Success("تم التحديث", $"تم تحديث السنة الدراسية «{result.NameAr}».");
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException invEx)
        {
            _toasts.Warning("تعذّر التحديث", invEx.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحديث السنة الدراسية", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeleteAcademicYearAsync(AcademicYear? year)
    {
        if (year is null) return;
        if (!CanManageSettings)
        {
            _toasts.Warning("لا توجد صلاحية", "ليس لديك صلاحية تعديل إعدادات المدرسة.");
            return;
        }

        var confirmed = await _dialogs
            .ConfirmDestructiveAsync(
                "حذف سنة دراسية",
                $"سيتم حذف السنة الدراسية «{year.NameAr}» نهائياً. هل أنت متأكد؟")
            .ConfigureAwait(true);
        if (!confirmed) return;

        try
        {
            await _busy.RunAsync(async () =>
            {
                await _repo.DeleteAcademicYearAsync(year.Id, CancellationToken.None).ConfigureAwait(true);
            }, "جاري حذف السنة الدراسية…").ConfigureAwait(true);

            _toasts.Success("تم الحذف", $"تم حذف السنة الدراسية «{year.NameAr}».");
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException invEx)
        {
            // Repo throws this for "current year" / "still referenced" cases —
            // surface them as a Warning toast, NOT an error window.
            _toasts.Warning("تعذّر الحذف", invEx.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف السنة الدراسية", ex.Message, ex);
        }
    }

    // ─── Phase 14 — Licensing card ──────────────────────────────────────
    [ObservableProperty] private string _licenseStatusText = "—";
    [ObservableProperty] private string _licenseCustomerName = "—";
    [ObservableProperty] private string _licenseEdition = "—";
    [ObservableProperty] private string _licenseExpiry = "—";
    [ObservableProperty] private string _licenseStatusKind = "Info";

    private void RefreshLicenseInfo()
    {
        var status = _license.Status;
        switch (status)
        {
            case LicenseStatus.Activated act:
                LicenseStatusText = "مفعَّل";
                LicenseStatusKind = "Activated";
                LicenseCustomerName = act.License?.CustomerName ?? "—";
                LicenseEdition = act.License?.Edition.ToString() ?? "—";
                LicenseExpiry = act.License?.ExpiresAtUtc.HasValue == true
                    ? act.License.ExpiresAtUtc!.Value.ToLocalTime().ToString("yyyy-MM-dd")
                    : "بدون انتهاء";
                break;
            case LicenseStatus.Trial t:
                LicenseStatusText = $"تجربة — {t.DaysRemaining} يوم متبقٍ";
                LicenseStatusKind = t.DaysRemaining <= 5 ? "Warning" : "Trial";
                LicenseCustomerName = "—";
                LicenseEdition = "تجربة";
                LicenseExpiry = "—";
                break;
            case LicenseStatus.Expired exp:
                LicenseStatusText = "منتهية الصلاحية";
                LicenseStatusKind = "Danger";
                LicenseCustomerName = exp.License?.CustomerName ?? "—";
                LicenseEdition = exp.License?.Edition.ToString() ?? "—";
                LicenseExpiry = exp.License?.ExpiresAtUtc?.ToLocalTime().ToString("yyyy-MM-dd") ?? "—";
                break;
            case LicenseStatus.Missing:
                LicenseStatusText = "غير مفعَّل";
                LicenseStatusKind = "Warning";
                LicenseCustomerName = "—";
                LicenseEdition = "—";
                LicenseExpiry = "—";
                break;
            default:
                LicenseStatusText = status?.GetType().Name ?? "—";
                LicenseStatusKind = "Danger";
                break;
        }
    }

    [RelayCommand]
    private void OpenActivation()
    {
        try
        {
            var win = _services.GetRequiredService<ActivationWindow>();
            win.DataContext = _services.GetRequiredService<ActivationViewModel>();
            win.Owner = Application.Current?.MainWindow;
            win.ShowDialog();
            RefreshLicenseInfo();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح نافذة التفعيل", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void CopyMachineId()
    {
        try
        {
            Clipboard.SetText(_license.MachineFingerprintBlock);
            _toasts.Success("تم النسخ", "تم نسخ بصمة الجهاز إلى الحافظة.");
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر نسخ بصمة الجهاز", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        var ok = await _dialogs.ConfirmDestructiveAsync(
            "إلغاء التفعيل",
            "سيُحذف ملف الترخيص من هذا الجهاز. هل أنت متأكد؟",
            okText: "إلغاء التفعيل").ConfigureAwait(true);
        if (!ok) return;

        try
        {
            _license.Deactivate();
            _toasts.Warning("تم إلغاء التفعيل", "أزيلت بيانات الترخيص. أعد تشغيل البرنامج لتطبيق التغيير.");
            RefreshLicenseInfo();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إلغاء التفعيل", ex.Message, ex);
        }
    }

    // ─── Phase 14 — Updates card ────────────────────────────────────────
    [ObservableProperty] private string _updateCurrentVersion = "—";
    [ObservableProperty] private string _updateLastCheckedText = "—";
    [ObservableProperty] private string _updateSourceText = "—";

    private void RefreshUpdateInfo()
    {
        UpdateCurrentVersion = _updates.CurrentVersion;
        UpdateLastCheckedText = _updates.LastCheckedUtc.HasValue
            ? _updates.LastCheckedUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "لم يُجرَ فحص بعد";
        UpdateSourceText = _updates.UpdateSource;
    }

    [RelayCommand]
    private void OpenUpdates()
    {
        try
        {
            var win = _services.GetRequiredService<UpdateWindow>();
            win.DataContext = _services.GetRequiredService<UpdateViewModel>();
            win.Owner = Application.Current?.MainWindow;
            win.ShowDialog();
            RefreshUpdateInfo();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح نافذة التحديثات", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void PickUpdateSource()
    {
        try
        {
            var dlg = new Nasag.Views.Licensing.UpdateSourceDialog(_updates.UpdateSource ?? "")
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultValue))
            {
                _updates.SetUpdateSource(dlg.ResultValue);
                _toasts.Success("تم الحفظ", $"مصدر التحديثات: {dlg.ResultValue}");
                RefreshUpdateInfo();
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تحديد مصدر التحديثات", ex.Message, ex);
        }
    }

    /// <summary>
    /// Opens the borderless first-run setup wizard so the user can re-configure
    /// the SQL Server connection string. On Save the wizard returns true and
    /// we surface a "restart to apply" dialog — the actual restart is the
    /// responsibility of <c>App.xaml.cs</c> (Agent A) when this wizard is opened
    /// at startup; here we only ask the user to relaunch manually.
    /// </summary>
    [RelayCommand]
    private async Task OpenDatabaseSetupAsync()
    {
        try
        {
            var wizard = _services.GetRequiredService<SetupWizardWindow>();
            wizard.DataContext = _services.GetRequiredService<SetupWizardViewModel>();
            wizard.Owner = System.Windows.Application.Current?.MainWindow;
            var ok = wizard.ShowDialog();
            if (ok == true)
            {
                var restart = await _dialogs.ConfirmAsync(
                    "تم حفظ إعدادات الاتصال",
                    "أعد تشغيل البرنامج لتطبيق التغييرات. هل تريد إعادة التشغيل الآن؟").ConfigureAwait(true);
                if (restart)
                {
                    _restarter.RestartNow();
                }
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح معالج إعداد قاعدة البيانات", ex.Message, ex);
        }
    }
}
