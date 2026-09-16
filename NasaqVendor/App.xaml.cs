using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NasaqVendor.Database;
using NasaqVendor.Repositories;
using NasaqVendor.Services;
using NasaqVendor.ViewModels;
using NasaqVendor.ViewModels.Dialogs;
using NasaqVendor.Views.Dialogs;

namespace NasaqVendor;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    private MainWindow? _main;

    public static T GetService<T>() where T : class
        => Host?.Services.GetRequiredService<T>()
           ?? throw new InvalidOperationException("Host is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) => ConfigureServices(services))
            .Build();

        await Host.StartAsync();

        DispatcherUnhandledException += (_, args) =>
        {
            var dlg = GetService<IDialogService>();
            dlg.Info("خطأ غير متوقع", args.Exception.Message, DialogKind.Danger);
            args.Handled = true;
        };

        _main = new MainWindow
        {
            DataContext = GetService<MainViewModel>()
        };
        MainWindow = _main;
        _main.Loaded += OnMainWindowLoaded;
        _main.Closed += (_, _) => Shutdown(0);
        _main.Show();
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_main is null) return;
        _main.Loaded -= OnMainWindowLoaded;

        // Wire the toast host
        var toasts = GetService<IToastService>() as ToastService;
        toasts?.Register(_main.GetToastHost());

        var keys = GetService<IIssuerKeyService>();
        await keys.EnsureLoadedAsync();

        if (!keys.HasKey)
        {
            // Show first-run initial key setup dialog
            var vm = new InitialKeySetupViewModel(keys, GetService<IFileService>());
            var dlg = new InitialKeySetupDialog
            {
                DataContext = vm,
                Owner = _main
            };
            dlg.ShowDialog();
        }

        // Initial load
        if (_main.DataContext is MainViewModel main)
            await main.Customers.LoadAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Host is not null)
        {
            await Host.StopAsync();
            Host.Dispose();
            Host = null;
        }
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<VendorDb>();

        services.AddSingleton<ICustomersRepository, CustomersRepository>();
        services.AddSingleton<ILicensesRepository, LicensesRepository>();
        services.AddSingleton<IIssueAuditRepository, IssueAuditRepository>();

        services.AddSingleton<IIssuerKeyService, IssuerKeyService>();
        services.AddSingleton<ILicenseIssuer, LicenseIssuer>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IToastService, ToastService>();

        services.AddSingleton<CustomersViewModel>();
        services.AddSingleton<LicensesViewModel>();
        services.AddSingleton<KeySettingsViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
