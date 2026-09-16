using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Licensing.License;
using NasaqVendor.Models;
using NasaqVendor.Repositories;
using NasaqVendor.Services;

namespace NasaqVendor.ViewModels.Dialogs;

public partial class FeatureChoice : ObservableObject
{
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string label = "";
    [ObservableProperty] private bool isSelected;
}

public partial class IssueLicenseViewModel : ObservableObject
{
    private static readonly Regex HexLine = new("^[0-9a-fA-F]{64}$", RegexOptions.Compiled);

    private readonly ICustomersRepository _customers;
    private readonly ILicenseIssuer _issuer;
    private readonly IFileService _files;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<FeatureChoice> Features { get; } = new();
    public ObservableCollection<LicenseEdition> Editions { get; } = new()
    {
        LicenseEdition.Standard, LicenseEdition.Pro, LicenseEdition.Trial
    };

    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private string machineHashesText = "";
    [ObservableProperty] private LicenseEdition edition = LicenseEdition.Standard;
    [ObservableProperty] private DateTime? expiresAtLocal;
    [ObservableProperty] private bool noExpiration = true;
    [ObservableProperty] private string? hashValidationText;
    [ObservableProperty] private bool hashesValid;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string customerSearch = "";

    public event EventHandler<bool>? RequestClose;

    public IssueLicenseViewModel(ICustomersRepository customers, ILicenseIssuer issuer, IFileService files)
    {
        _customers = customers;
        _issuer = issuer;
        _files = files;

        // Default feature set per spec
        string[] defaults = { "fees", "reports", "backup", "attendance", "marks", "results", "exam", "users", "settings" };
        foreach (var code in defaults)
        {
            Features.Add(new FeatureChoice
            {
                Code = code,
                Label = ToArabic(code),
                IsSelected = true
            });
        }
    }

    private static string ToArabic(string code) => code switch
    {
        "fees" => "الرسوم",
        "reports" => "التقارير",
        "backup" => "النسخ الاحتياطي",
        "attendance" => "الحضور",
        "marks" => "الدرجات",
        "results" => "النتائج",
        "exam" => "الامتحانات",
        "users" => "المستخدمون",
        "settings" => "الإعدادات",
        _ => code
    };

    public async Task LoadCustomersAsync()
    {
        var all = await _customers.ListAsync();
        Customers.Clear();
        foreach (var c in all) Customers.Add(c);
    }

    partial void OnMachineHashesTextChanged(string value) => ValidateHashes();

    private void ValidateHashes()
    {
        var lines = (MachineHashesText ?? "")
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length == 0)
        {
            HashesValid = false;
            HashValidationText = "أدخل بصمات الجهاز (سطر لكل بصمة، 64 خانة hex).";
            return;
        }

        var bad = lines.Where(l => !HexLine.IsMatch(l)).ToArray();
        if (bad.Length > 0)
        {
            HashesValid = false;
            HashValidationText = $"✗ يوجد {bad.Length} سطر(أسطر) غير صالحة. يجب أن يكون كل سطر 64 خانة hex.";
            return;
        }

        if (lines.Length != 5)
        {
            HashesValid = false;
            HashValidationText = $"عدد الأسطر = {lines.Length}. المطلوب 5 بصمات بالضبط.";
            return;
        }

        HashesValid = true;
        HashValidationText = "✓ 5 أسطر صحيحة.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedCustomer is null)
        {
            ErrorMessage = "اختر العميل.";
            return;
        }
        ValidateHashes();
        if (!HashesValid)
        {
            ErrorMessage = "بصمات الجهاز غير صالحة.";
            return;
        }
        if (!NoExpiration && ExpiresAtLocal is null)
        {
            ErrorMessage = "حدّد تاريخ الانتهاء أو فعّل «بلا انتهاء».";
            return;
        }

        var hashes = (MachineHashesText ?? "")
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().ToLowerInvariant())
            .ToArray();

        var features = Features.Where(f => f.IsSelected).Select(f => f.Code).ToArray();

        var defaultName = $"{SelectedCustomer.Code}-{DateTime.Now:yyyyMMdd}.naslic";
        var path = _files.SaveFile(
            "حفظ ملف الترخيص (.naslic)",
            defaultName,
            "ملفات الترخيص (*.naslic)|*.naslic|كل الملفات (*.*)|*.*",
            ".naslic");
        if (string.IsNullOrEmpty(path)) return;

        DateTime? expiresUtc = null;
        if (!NoExpiration && ExpiresAtLocal.HasValue)
            expiresUtc = ExpiresAtLocal.Value.ToUniversalTime();

        try
        {
            IsBusy = true;
            await _issuer.IssueAsync(new IssueLicenseRequest
            {
                CustomerId = SelectedCustomer.Id,
                Edition = Edition,
                MachineHashes = hashes,
                Features = features,
                ExpiresAtUtc = expiresUtc,
                TargetFilePath = path
            });
            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);
}
