using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Services;

namespace NasaqVendor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public CustomersViewModel Customers { get; }
    public LicensesViewModel Licenses { get; }
    public KeySettingsViewModel KeySettings { get; }

    [ObservableProperty]
    private object? currentPage;

    [ObservableProperty]
    private string activePage = "customers";

    [ObservableProperty]
    private bool hasKey;

    private readonly IIssuerKeyService _keyService;

    public MainViewModel(
        CustomersViewModel customers,
        LicensesViewModel licenses,
        KeySettingsViewModel keySettings,
        IIssuerKeyService keyService)
    {
        Customers = customers;
        Licenses = licenses;
        KeySettings = keySettings;
        _keyService = keyService;
        _keyService.KeyChanged += (_, _) => HasKey = _keyService.HasKey;
        HasKey = _keyService.HasKey;
        CurrentPage = Customers;
    }

    [RelayCommand]
    private void ShowCustomers()
    {
        ActivePage = "customers";
        CurrentPage = Customers;
        _ = Customers.LoadAsync();
    }

    [RelayCommand]
    private void ShowLicenses()
    {
        ActivePage = "licenses";
        CurrentPage = Licenses;
        _ = Licenses.LoadAsync();
    }

    [RelayCommand]
    private void ShowKeySettings()
    {
        ActivePage = "keys";
        CurrentPage = KeySettings;
        KeySettings.Refresh();
    }
}
