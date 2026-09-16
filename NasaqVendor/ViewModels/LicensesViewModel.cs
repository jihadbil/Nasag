using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Models;
using NasaqVendor.Repositories;
using NasaqVendor.Services;
using NasaqVendor.ViewModels.Dialogs;
using NasaqVendor.Views.Dialogs;

namespace NasaqVendor.ViewModels;

public partial class LicensesViewModel : ObservableObject
{
    private readonly ILicensesRepository _repo;
    private readonly IIssueAuditRepository _auditRepo;
    private readonly ICustomersRepository _customersRepo;
    private readonly ILicenseIssuer _issuer;
    private readonly IIssuerKeyService _keys;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IFileService _files;

    public ObservableCollection<LicenseRecord> Items { get; } = new();
    public ObservableCollection<string> StatusFilters { get; } = new() { "الكل", "نشط", "مُبطَل" };

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string statusFilter = "الكل";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int activeCount;
    [ObservableProperty] private int revokedCount;
    [ObservableProperty] private bool isBusy;

    public bool CanIssue => _keys.HasKey;

    public LicensesViewModel(
        ILicensesRepository repo,
        IIssueAuditRepository auditRepo,
        ICustomersRepository customersRepo,
        ILicenseIssuer issuer,
        IIssuerKeyService keys,
        IDialogService dialogs,
        IToastService toasts,
        IFileService files)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _customersRepo = customersRepo;
        _issuer = issuer;
        _keys = keys;
        _dialogs = dialogs;
        _toasts = toasts;
        _files = files;
        _keys.KeyChanged += (_, _) => OnPropertyChanged(nameof(CanIssue));
    }

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();
    partial void OnStatusFilterChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var filter = StatusFilter switch
            {
                "نشط" => "Active",
                "مُبطَل" => "Revoked",
                _ => null
            };
            var rows = await _repo.ListAsync(SearchText, filter);
            Items.Clear();
            foreach (var r in rows) Items.Add(r);
            TotalCount = Items.Count;
            var counts = await _repo.GetCountsAsync();
            ActiveCount = counts.Active;
            RevokedCount = counts.Revoked;
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر تحميل التراخيص: {ex.Message}", DialogKind.Danger);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task IssueAsync()
    {
        if (!_keys.HasKey)
        {
            _dialogs.Info("لا يوجد مفتاح",
                "يجب توليد أو استيراد مفاتيح الإصدار أولاً من «إعدادات المفتاح».",
                DialogKind.Warning);
            return;
        }

        var vm = new IssueLicenseViewModel(_customersRepo, _issuer, _files);
        await vm.LoadCustomersAsync();
        var dlg = new IssueLicenseDialog
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() == true)
        {
            await LoadAsync();
            _toasts.Show("تم إصدار الترخيص.", ToastKind.Success);
        }
    }

    [RelayCommand]
    private async Task RevokeAsync(LicenseRecord? rec)
    {
        if (rec is null) return;
        if (rec.Revoked)
        {
            _dialogs.Info("ملاحظة", "هذا الترخيص مُبطَل بالفعل.", DialogKind.Info);
            return;
        }

        var vm = new RevokeLicenseViewModel(rec);
        var dlg = new RevokeLicenseDialog
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _repo.RevokeAsync(rec.Id);
            await _auditRepo.InsertAsync(new IssueAudit
            {
                LicenseId = rec.Id,
                Action = "Revoked",
                AtUtc = DateTime.UtcNow,
                Operator = Environment.UserName,
                Notes = string.IsNullOrWhiteSpace(vm.Reason) ? null : vm.Reason
            });
            await LoadAsync();
            _toasts.Show("تم إبطال الترخيص.", ToastKind.Warning);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر الإبطال: {ex.Message}", DialogKind.Danger);
        }
    }

    [RelayCommand]
    private async Task ViewAuditAsync(LicenseRecord? rec)
    {
        if (rec is null) return;
        try
        {
            var rows = await _auditRepo.ListForLicenseAsync(rec.Id);
            var vm = new AuditLogViewModel(rec, rows);
            var dlg = new AuditLogDialog
            {
                DataContext = vm,
                Owner = Application.Current?.MainWindow
            };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر تحميل سجل التدقيق: {ex.Message}", DialogKind.Danger);
        }
    }

    [RelayCommand]
    private async Task ReExportAsync(LicenseRecord? rec)
    {
        if (rec is null) return;
        if (!_keys.HasKey)
        {
            _dialogs.Info("لا يوجد مفتاح", "أنشئ المفاتيح أولاً.", DialogKind.Warning);
            return;
        }

        var defaultName = $"{rec.CustomerCode}-{DateTime.Now:yyyyMMdd}.naslic";
        var path = _files.SaveFile("حفظ ملف الترخيص",
            defaultName,
            "ملفات الترخيص (*.naslic)|*.naslic|كل الملفات (*.*)|*.*",
            ".naslic");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _issuer.ReExportAsync(rec.Id, path);
            await LoadAsync();
            _toasts.Show("تم إعادة تصدير الترخيص.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر إعادة التصدير: {ex.Message}", DialogKind.Danger);
        }
    }
}
