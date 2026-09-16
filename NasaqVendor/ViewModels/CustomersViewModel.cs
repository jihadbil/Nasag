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

public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomersRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;

    public ObservableCollection<Customer> Items { get; } = new();

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private bool isBusy;

    public CustomersViewModel(ICustomersRepository repo, IDialogService dialogs, IToastService toasts)
    {
        _repo = repo;
        _dialogs = dialogs;
        _toasts = toasts;
    }

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var rows = await _repo.ListAsync(SearchText);
            Items.Clear();
            foreach (var r in rows) Items.Add(r);
            TotalCount = Items.Count;
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر تحميل العملاء: {ex.Message}", DialogKind.Danger);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var vm = new CustomerEditorViewModel(_repo);
        await vm.PrepareForAddAsync();
        var dlg = new CustomerEditorDialog
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() == true)
        {
            await LoadAsync();
            _toasts.Show("تم إضافة العميل.", ToastKind.Success);
        }
    }

    [RelayCommand]
    private async Task EditAsync(Customer? customer)
    {
        if (customer is null) return;
        var vm = new CustomerEditorViewModel(_repo);
        await vm.PrepareForEditAsync(customer.Id);
        var dlg = new CustomerEditorDialog
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() == true)
        {
            await LoadAsync();
            _toasts.Show("تم تحديث بيانات العميل.", ToastKind.Success);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Customer? customer)
    {
        if (customer is null) return;
        try
        {
            var licenses = await _repo.CountLicensesAsync(customer.Id);
            if (licenses > 0)
            {
                _dialogs.Info("غير مسموح",
                    "لا يمكن حذف عميل لديه تراخيص. أبطل التراخيص أولاً.",
                    DialogKind.Warning);
                return;
            }

            if (!_dialogs.Confirm("حذف العميل",
                $"هل تريد حذف العميل «{customer.Name}»؟ لا يمكن التراجع.",
                okText: "حذف",
                kind: DialogKind.Danger))
                return;

            await _repo.DeleteAsync(customer.Id);
            await LoadAsync();
            _toasts.Show("تم حذف العميل.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر الحذف: {ex.Message}", DialogKind.Danger);
        }
    }
}
