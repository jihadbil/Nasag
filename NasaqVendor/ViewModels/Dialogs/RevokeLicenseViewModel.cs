using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Models;

namespace NasaqVendor.ViewModels.Dialogs;

public partial class RevokeLicenseViewModel : ObservableObject
{
    [ObservableProperty] private string customerName = "";
    [ObservableProperty] private string licenseLabel = "";
    [ObservableProperty] private string? reason;

    public event EventHandler<bool>? RequestClose;

    public RevokeLicenseViewModel(LicenseRecord rec)
    {
        CustomerName = rec.CustomerName;
        LicenseLabel = $"رقم الترخيص #{rec.Id} — {rec.Edition}";
    }

    [RelayCommand]
    private void Confirm() => RequestClose?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);
}
