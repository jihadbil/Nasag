using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Nasag.Licensing.License;
using Nasag.Services;
using Nasag.Services.Licensing;

namespace Nasag.ViewModels.Licensing;

/// <summary>
/// معالج تفعيل من 4 خطوات: ترحيب → بصمة الجهاز → تحميل/لصق الترخيص → تأكيد.
/// </summary>
public sealed partial class ActivationViewModel : ObservableObject
{
    private readonly ILicenseService _license;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly IApplicationRestarter _restarter;

    public ActivationViewModel(
        ILicenseService license,
        IToastService toasts,
        IErrorReporter errors,
        IApplicationRestarter restarter)
    {
        _license = license;
        _toasts = toasts;
        _errors = errors;
        _restarter = restarter;

        var hashes = _license.CurrentMachineHashes;
        CpuHash = hashes.Length > 0 ? hashes[0] : "";
        BoardHash = hashes.Length > 1 ? hashes[1] : "";
        BiosHash = hashes.Length > 2 ? hashes[2] : "";
        MachineHash = hashes.Length > 3 ? hashes[3] : "";
        MacHash = hashes.Length > 4 ? hashes[4] : "";
        FingerprintBlock = _license.MachineFingerprintBlock;
    }

    /// <summary>طلب الإغلاق من النافذة (للمعالج).</summary>
    public Action<bool?>? RequestClose { get; set; }

    // ─── الخطوة ──────────────────────────────────────────────────────────
    [ObservableProperty] private int _currentStep;

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep0Active => CurrentStep >= 0;
    public bool IsStep1Active => CurrentStep >= 1;
    public bool IsStep2Active => CurrentStep >= 2;
    public bool IsStep3Active => CurrentStep >= 3;
    public bool IsFirstStep => CurrentStep == 0;
    public bool IsLastStep => CurrentStep == 3;

    public string StepIndicatorText => $"الخطوة {CurrentStep + 1} من 4";

    public string NextButtonText => CurrentStep switch
    {
        0 => "متابعة",
        1 => "متابعة",
        2 => "متابعة",
        3 => "دخول البرنامج",
        _ => "متابعة"
    };

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep0Active));
        OnPropertyChanged(nameof(IsStep1Active));
        OnPropertyChanged(nameof(IsStep2Active));
        OnPropertyChanged(nameof(IsStep3Active));
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepIndicatorText));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(CanGoNext));
    }

    // ─── الخطوة 0: نيّة المستخدم ────────────────────────────────────────
    [ObservableProperty] private bool _hasLicenseFile = true;
    [ObservableProperty] private bool _needsNewLicense;

    partial void OnHasLicenseFileChanged(bool value) { if (value) NeedsNewLicense = false; }
    partial void OnNeedsNewLicenseChanged(bool value) { if (value) HasLicenseFile = false; }

    // ─── الخطوة 1: بصمة الجهاز ──────────────────────────────────────────
    [ObservableProperty] private string _cpuHash = "";
    [ObservableProperty] private string _boardHash = "";
    [ObservableProperty] private string _biosHash = "";
    [ObservableProperty] private string _machineHash = "";
    [ObservableProperty] private string _macHash = "";
    [ObservableProperty] private string _fingerprintBlock = "";
    [ObservableProperty] private string? _schoolName;

    // ─── الخطوة 2: تحميل/لصق الترخيص ─────────────────────────────────────
    [ObservableProperty] private string? _licenseFilePath;
    [ObservableProperty] private string? _pastedLicenseJson;
    [ObservableProperty] private string? _verificationError;
    [ObservableProperty] private bool _hasVerifiedLicense;
    [ObservableProperty] private string? _verifiedCustomerName;
    [ObservableProperty] private string? _verifiedEdition;
    [ObservableProperty] private string? _verifiedFeatures;
    [ObservableProperty] private string? _verifiedIssuedAt;
    [ObservableProperty] private string? _verifiedExpiresAt;
    [ObservableProperty] private bool _isVerifying;

    partial void OnHasVerifiedLicenseChanged(bool value) => OnPropertyChanged(nameof(CanGoNext));
    partial void OnLicenseFilePathChanged(string? value)
    {
        HasVerifiedLicense = false;
        VerificationError = null;
    }

    public bool CanGoNext => CurrentStep switch
    {
        0 => HasLicenseFile || NeedsNewLicense,
        1 => true,
        2 => HasVerifiedLicense,
        3 => true,
        _ => false
    };

    [RelayCommand]
    private void PrevStep()
    {
        if (CurrentStep > 0) CurrentStep--;
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (!CanGoNext) return;

        // عند الانتقال للخطوة الأخيرة من خطوة التحقق نُحاول النسخ والتفعيل.
        if (CurrentStep == 3)
        {
            // إغلاق المعالج عبر الزر «دخول البرنامج».
            try { RequestClose?.Invoke(true); } catch { /* تجاهل */ }
            // إعادة تشغيل عبر السلسلة (يتم استدعاؤها أيضاً من بوابة الترخيص).
            _restarter.RestartNow();
            return;
        }

        await Task.CompletedTask.ConfigureAwait(true);
        CurrentStep++;
    }

    [RelayCommand]
    private void Cancel()
    {
        try { RequestClose?.Invoke(false); } catch { /* تجاهل */ }
    }

    [RelayCommand]
    private void CopyFingerprint()
    {
        try
        {
            Clipboard.SetText(FingerprintBlock + (string.IsNullOrWhiteSpace(SchoolName) ? "" : $"\nSchool: {SchoolName}"));
            _toasts.Success("تم النسخ", "تم نسخ بصمة الجهاز إلى الحافظة.");
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر نسخ بصمة الجهاز", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void PickLicenseFile()
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Title = "اختر ملف الترخيص",
                Filter = "ملفات ترخيص نَسَق (*.naslic)|*.naslic|كل الملفات (*.*)|*.*",
                CheckFileExists = true,
            };
            if (dlg.ShowDialog() == true)
            {
                LicenseFilePath = dlg.FileName;
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح متصفّح الملفات", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task VerifyAsync()
    {
        VerificationError = null;
        HasVerifiedLicense = false;
        IsVerifying = true;
        try
        {
            ActivationResult result;
            if (!string.IsNullOrWhiteSpace(LicenseFilePath))
            {
                result = await _license.ActivateAsync(LicenseFilePath!).ConfigureAwait(true);
            }
            else if (!string.IsNullOrWhiteSpace(PastedLicenseJson))
            {
                result = await _license.ActivateFromTextAsync(PastedLicenseJson!).ConfigureAwait(true);
            }
            else
            {
                VerificationError = "اختر ملف ترخيص أو الصق محتواه.";
                return;
            }

            if (!result.Success)
            {
                VerificationError = result.Message;
                return;
            }

            if (result.NewStatus is LicenseStatus.Activated act && act.License is { } license)
            {
                VerifiedCustomerName = license.CustomerName;
                VerifiedEdition = license.Edition.ToString();
                VerifiedFeatures = license.Features is { Length: > 0 }
                    ? string.Join("، ", license.Features)
                    : "—";
                VerifiedIssuedAt = license.IssuedAtUtc.ToLocalTime().ToString("yyyy-MM-dd");
                VerifiedExpiresAt = license.ExpiresAtUtc.HasValue
                    ? license.ExpiresAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd")
                    : "بدون انتهاء";
                HasVerifiedLicense = true;

                // ننتقل تلقائياً إلى الخطوة الأخيرة.
                CurrentStep = 3;
            }
            else
            {
                VerificationError = result.Message;
            }
        }
        catch (Exception ex)
        {
            VerificationError = $"تعذّر التحقّق: {ex.Message}";
        }
        finally
        {
            IsVerifying = false;
        }
    }
}
