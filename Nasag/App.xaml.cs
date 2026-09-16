using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nasag.Data;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.Services.Licensing;
using Nasag.ViewModels.Auth;
using Nasag.ViewModels.Licensing;
using Nasag.ViewModels.Pages;
using Nasag.ViewModels.Pages.Attendance;
using Nasag.ViewModels.Pages.Classes;
using Nasag.ViewModels.Pages.Exams;
using Nasag.ViewModels.Pages.Fees;
using Nasag.ViewModels.Pages.Marks;
using Nasag.ViewModels.Pages.Reports;
using Nasag.ViewModels.Pages.Results;
using Nasag.ViewModels.Pages.Students;
using Nasag.ViewModels.Pages.Subjects;
using Nasag.ViewModels.Pages.Settings;
using Nasag.ViewModels.Pages.Users;
using Nasag.ViewModels.Pages.Backup;
using Nasag.ViewModels.Splash;
using Nasag.Services.Reports;
using Nasag.ViewModels.Shell;
using Nasag.Views.Auth;
using Nasag.Views.Licensing;
using Nasag.Views.Shell;
using Nasag.Views.Splash;
using Velopack;
using LicensingStatus = Nasag.Licensing.License.LicenseStatus;

namespace Nasag;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    private LoginView? _loginWindow;
    private MainShellView? _shellWindow;
    private SplashWindow? _splashWindow;
    private LicenseGateWindow? _gateWindow;
    private ILicenseService? _licenseService;

    public static T GetService<T>() where T : class
        => Host?.Services.GetRequiredService<T>()
           ?? throw new InvalidOperationException("Host is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        LogTrace("App.OnStartup entered!");
        // Velopack must run before any other startup logic so its hook arguments
        // (--veloapp-*) can short-circuit when invoked by the updater/installer.
        // Velopack initialization (enabled when running in packaged production build)
        // try { VelopackApp.Build().Run(); } catch { }

        base.OnStartup(e);

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) => ConfigureServices(services, ctx.Configuration))
            .Build();

        await Host.StartAsync();

        // Global exception handlers — funnel everything through ErrorReporter.
        var reporter = GetService<IErrorReporter>();
        DispatcherUnhandledException += (_, args) =>
        {
            reporter.Report("خطأ غير متوقع", args.Exception.Message, args.Exception);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                reporter.Report("خطأ غير متوقع", ex.Message, ex);
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            reporter.Report("خطأ في مهمة خلفية", args.Exception.Message, args.Exception);
            args.SetObserved();
        };

        // Cache the license service so all license-gate paths share one instance.
        _licenseService = GetService<ILicenseService>();

        // 1) Show splash first; it runs InitializeAsync and signals back via Completed.
        var splashVm = GetService<SplashViewModel>();
        splashVm.Completed += OnSplashCompleted;

        _splashWindow = new SplashWindow
        {
            DataContext = splashVm
        };
        MainWindow = _splashWindow;
        _splashWindow.Show();

        // Fire-and-forget the init; the VM raises Completed when done (success or terminal error).
        _ = splashVm.RunInitAsync();
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

    private static void LogTrace(string msg)
    {
        try
        {
            var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nasaq");
            System.IO.Directory.CreateDirectory(logDir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(logDir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }

    private void OnSplashCompleted(object? sender, SplashResult result)
    {
        LogTrace($"OnSplashCompleted status={result.Status}, err={result.ErrorMessage}");
        Dispatcher.Invoke(() =>
        {
            switch (result.Status)
            {
                case DatabaseInitStatus.Success:
                    var status = _licenseService?.GetStatusOnStartup();
                    LogTrace($"License status={status?.GetType().Name}");
                    if (status is not (LicensingStatus.Activated or LicensingStatus.Trial))
                    {
                        LogTrace("Showing License Gate...");
                        ShowLicenseGate(status);
                        CloseSplash();
                        return;
                    }

                    LogTrace("Showing Login Window...");
                    WireAuthLifecycleAndShowLogin();
                    CloseSplash();
                    break;

                case DatabaseInitStatus.CannotConnect:
                    LogTrace("Showing Setup Wizard...");
                    ShowSetupWizard();
                    CloseSplash();
                    break;

                default:
                    LogTrace("Terminal DB error on splash");
                    break;
            }
        });
    }

    private void ShowLicenseGate(LicensingStatus? status)
    {
        try
        {
            var vm = GetService<LicenseGateViewModel>();
            if (status is not null) vm.SetStatus(status);
            _gateWindow = GetService<LicenseGateWindow>();
            _gateWindow.DataContext = vm;
            MainWindow = _gateWindow;
            _gateWindow.Show();
        }
        catch (Exception ex)
        {
            var reporter = GetService<IErrorReporter>();
            reporter.Report("تعذّر فتح بوابة الترخيص", ex.Message, ex);
            Shutdown(-1);
        }
    }

    private void WireAuthLifecycleAndShowLogin()
    {
        var currentUser = GetService<ICurrentUserService>();
        currentUser.SignedIn += OnSignedIn;
        currentUser.SignedOut += OnSignedOut;
        ShowLoginWindow();
    }

    private void ShowSetupWizard()
    {
        while (true)
        {
            try
            {
                var wizard = GetService<Nasag.Views.Setup.SetupWizardWindow>();
                wizard.DataContext = GetService<Nasag.ViewModels.Setup.SetupWizardViewModel>();
                var ok = wizard.ShowDialog();
                if (ok == true)
                {
                    Nasag.Views.Common.NasaqDialog.Show(
                        null,
                        "نَسَق — إعادة التشغيل",
                        "تم حفظ إعدادات الاتصال. سيُعاد تشغيل البرنامج الآن.",
                        Nasag.Views.Common.NasaqDialogKind.Success);
                    GetService<IApplicationRestarter>().RestartNow();
                    return;
                }

                var quit = Nasag.Views.Common.NasaqDialog.Confirm(
                    null,
                    "إغلاق البرنامج",
                    "لم يتم إعداد قاعدة بيانات. لا يمكن متابعة تشغيل البرنامج دون قاعدة. هل تريد إغلاق البرنامج الآن؟",
                    okText: "إغلاق البرنامج",
                    cancelText: "العودة للمعالج",
                    kind: Nasag.Views.Common.NasaqDialogKind.Warning);

                if (quit)
                {
                    Shutdown(0);
                    return;
                }
            }
            catch (Exception ex)
            {
                var reporter = GetService<IErrorReporter>();
                reporter.Report("تعذّر فتح معالج الإعداد", ex.Message, ex);
                Shutdown(-1);
                return;
            }
        }
    }

    private void CloseSplash()
    {
        if (_splashWindow is null) return;
        try { _splashWindow.Close(); } catch { /* ignore */ }
        _splashWindow = null;
    }

    private void ShowLoginWindow()
    {
        LogTrace("ShowLoginWindow creating LoginView");
        _loginWindow = new LoginView
        {
            DataContext = GetService<LoginViewModel>()
        };
        _loginWindow.Closed += OnLoginClosed;
        MainWindow = _loginWindow;
        _loginWindow.Show();
        LogTrace("ShowLoginWindow shown successfully");
    }

    private void OnSignedIn(object? sender, EventArgs e)
    {
        LogTrace("OnSignedIn triggered");
        // Spin up the shell, swap MainWindow, then close the login window.
        _shellWindow = new MainShellView
        {
            DataContext = GetService<MainShellViewModel>()
        };
        _shellWindow.Closed += OnShellClosed;
        MainWindow = _shellWindow;
        _shellWindow.Show();

        if (_loginWindow is not null)
        {
            _loginWindow.Closed -= OnLoginClosed;
            _loginWindow.Close();
            _loginWindow = null;
        }

        // Fire-and-forget background update check 5 seconds after shell is up.
        _ = ScheduleStartupUpdateCheckAsync();
    }

    private async Task ScheduleStartupUpdateCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            var updates = GetService<IUpdateService>();
            var toasts = GetService<IToastService>();
            var result = await updates.CheckAsync().ConfigureAwait(true);
            if (result.HasUpdate && !string.IsNullOrWhiteSpace(result.NewVersion))
            {
                toasts.Info(
                    "تتوفر نسخة جديدة",
                    $"النسخة {result.NewVersion} متاحة. افتح «الإعدادات → التحديثات» للتفاصيل.");
            }
        }
        catch
        {
            // فحص بدء التشغيل يجب ألا يفسد التطبيق.
        }
    }

    private void OnSignedOut(object? sender, EventArgs e)
    {
        LogTrace("OnSignedOut triggered");
        ShowLoginWindow();

        if (_shellWindow is not null)
        {
            _shellWindow.Closed -= OnShellClosed;
            _shellWindow.Close();
            _shellWindow = null;
        }
    }

    private void OnLoginClosed(object? sender, EventArgs e)
    {
        LogTrace("OnLoginClosed triggered!");
        // User dismissed the login window without signing in -> exit the app.
        if (Current is not null) Current.Shutdown(0);
    }

    private void OnShellClosed(object? sender, EventArgs e)
    {
        // Closing the shell directly (X button) ends the session.
        if (Current is not null) Current.Shutdown(0);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var registry = new ConnectionRegistry(configuration);
        services.AddSingleton<IConnectionRegistry>(registry);
        services.AddSingleton<IServerDiscoveryService, ServerDiscoveryService>();
        services.AddSingleton<IApplicationRestarter, ApplicationRestarter>();

        services.AddDbContextFactory<NasaqDbContext>(options =>
        {
            options.UseSqlServer(registry.ActiveConnectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(NasaqDbContext).Assembly.GetName().Name);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        // Data services
        services.AddSingleton<IPendingAdminSetupStore, PendingAdminSetupStore>();
        services.AddSingleton<IDbSeeder, DbSeeder>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<IStudentsRepository, StudentsRepository>();
        services.AddSingleton<IClassesRepository, ClassesRepository>();
        services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
        services.AddSingleton<ISubjectsRepository, SubjectsRepository>();
        services.AddSingleton<IExamsRepository, ExamsRepository>();
        services.AddSingleton<IMarksRepository, MarksRepository>();
        services.AddSingleton<IResultsRepository, ResultsRepository>();
        services.AddSingleton<IFeesRepository, FeesRepository>();
        services.AddSingleton<IReportsRepository, ReportsRepository>();
        services.AddSingleton<IResultsCalculator, ResultsCalculator>();
        services.AddSingleton<IReportPdfService, ReportPdfService>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IUsersRepository, UsersRepository>();
        services.AddSingleton<IBackupsRepository, BackupsRepository>();
        services.AddSingleton<IBackupService, BackupService>();

        // Cross-cutting services
        services.AddSingleton<IAppInfoService, AppInfoService>();
        services.AddSingleton<IBusyService, BusyService>();
        services.AddSingleton<IConnectionMonitor, ConnectionMonitor>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IErrorReporter, ErrorReporter>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<IExcelService, ExcelService>();

        // Phase 14 — Licensing + Updates
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddTransient<LicenseGateWindow>();
        services.AddTransient<ActivationWindow>();
        services.AddTransient<UpdateWindow>();
        services.AddTransient<LicenseGateViewModel>();
        services.AddTransient<ActivationViewModel>();
        services.AddTransient<UpdateViewModel>();

        // Phase 13 — Splash + Setup Wizard
        services.AddSingleton<Nasag.Services.IConnectionTester, Nasag.Services.ConnectionTester>();
        services.AddSingleton<SplashViewModel>();
        services.AddTransient<Nasag.ViewModels.Setup.SetupWizardViewModel>();
        services.AddTransient<Nasag.Views.Setup.SetupWizardWindow>();

        // Auth
        services.AddTransient<LoginViewModel>();

        // Shell
        services.AddSingleton<MainShellViewModel>();

        // Page VMs (singletons so they keep their state during the session)
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<StudentEditorViewModel>();
        services.AddTransient<StudentImportWizardViewModel>();
        services.AddSingleton<StudentsViewModel>();
        services.AddSingleton<ClassesViewModel>();
        services.AddSingleton<AttendanceViewModel>();
        services.AddSingleton<SubjectsViewModel>();
        services.AddSingleton<ExamsViewModel>();
        services.AddSingleton<MarksViewModel>();
        services.AddSingleton<ResultsViewModel>();
        services.AddSingleton<FeesViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddTransient<StudentsReportViewModel>();
        services.AddTransient<AttendanceReportViewModel>();
        services.AddTransient<MarksReportViewModel>();
        services.AddTransient<FeesReportViewModel>();
        services.AddSingleton<UsersViewModel>();
        services.AddTransient<Nasag.ViewModels.Pages.Users.UserEditorDialogViewModel>();
        services.AddTransient<Nasag.ViewModels.Pages.Users.PasswordResetDialogViewModel>();
        services.AddTransient<Nasag.ViewModels.Pages.Users.RoleEditorDialogViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<Nasag.ViewModels.Pages.Settings.AcademicYearDialogViewModel>();
        services.AddSingleton<BackupViewModel>();
        services.AddTransient<Nasag.ViewModels.Pages.Backup.BackupNotesDialogViewModel>();
    }
}
