using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Nasag.Services;

namespace Nasag.ViewModels.Setup;

/// <summary>
/// نية المستخدم في الخطوة 0: إنشاء قاعدة بيانات جديدة على الخادم
/// أم الاتصال بقاعدة بيانات نَسَق سبق إنشاؤها.
/// </summary>
public enum WizardIntent
{
    CreateNew = 0,
    UseExisting = 1
}

/// <summary>
/// نوع المصادقة المختار في خطوة الخادم.
/// </summary>
public enum WizardAuthMode
{
    Windows = 0,
    SqlAuth = 1
}

/// <summary>
/// View-model لمعالج الإعداد ذي الخمس خطوات (إعادة تصميم Phase 13):
///   0 — مرحباً + اختيار نية (إنشاء قاعدة جديدة / استخدام موجودة).
///   1 — اختيار الخادم (Auto-discovery + إدخال يدوي) + المصادقة.
///   2 — اختيار/إدخال قاعدة البيانات (قائمة من الخادم في وضع UseExisting).
///   3 — اختبار الاتصال (وفي وضع CreateNew إنشاء القاعدة عند الحاجة).
///   4 — اسم العرض + الحفظ.
/// </summary>
public sealed partial class SetupWizardViewModel : ObservableObject
{
    private readonly IConnectionRegistry _registry;
    private readonly IConnectionTester _tester;
    private readonly IServerDiscoveryService _discovery;
    private readonly IErrorReporter _errors;
    private readonly IPendingAdminSetupStore _pendingAdminStore;

    public const int TotalSteps = 5;

    /// <summary>
    /// يُضبط من code-behind ليغلق النافذة بنتيجة DialogResult المناسبة.
    /// </summary>
    public Action<bool>? RequestClose { get; set; }

    public SetupWizardViewModel(
        IConnectionRegistry registry,
        IConnectionTester tester,
        IServerDiscoveryService discovery,
        IErrorReporter errors,
        IPendingAdminSetupStore pendingAdminStore)
    {
        _registry = registry;
        _tester = tester;
        _discovery = discovery;
        _errors = errors;
        _pendingAdminStore = pendingAdminStore;
    }

    // ─── Step state ─────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(StepIndicatorText))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(IsStep0))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(IsStep0Active))]
    [NotifyPropertyChangedFor(nameof(IsStep1Active))]
    [NotifyPropertyChangedFor(nameof(IsStep2Active))]
    [NotifyPropertyChangedFor(nameof(IsStep3Active))]
    [NotifyPropertyChangedFor(nameof(IsStep4Active))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(SummaryConnectionString))]
    [NotifyPropertyChangedFor(nameof(SummaryDataSource))]
    [NotifyPropertyChangedFor(nameof(SummaryDatabase))]
    [NotifyPropertyChangedFor(nameof(SummaryAuthMode))]
    private int _currentStep = 0;

    public bool IsFirstStep => CurrentStep == 0;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;
    public string StepIndicatorText => $"الخطوة {CurrentStep + 1} من {TotalSteps}";
    public string NextButtonText => IsLastStep ? "حفظ وإعادة التشغيل" : "التالي";

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;

    // Stepper dot colors (Teal when reached or active).
    public bool IsStep0Active => CurrentStep >= 0;
    public bool IsStep1Active => CurrentStep >= 1;
    public bool IsStep2Active => CurrentStep >= 2;
    public bool IsStep3Active => CurrentStep >= 3;
    public bool IsStep4Active => CurrentStep >= 4;

    // ─── Step 0: intent ─────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreateNew))]
    [NotifyPropertyChangedFor(nameof(IsUseExisting))]
    [NotifyPropertyChangedFor(nameof(Step2TitleText))]
    [NotifyPropertyChangedFor(nameof(TestActionButtonText))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(AdminValidationError))]
    [NotifyPropertyChangedFor(nameof(HasAdminValidationError))]
    private WizardIntent _intent = WizardIntent.CreateNew;

    // Pre-selected to match the visually-checked default RadioButton (CreateNew).
    // The user can still switch to UseExisting before pressing Next; both choices
    // explicitly set this in the click handlers, but defaulting to true avoids the
    // confusing "checked radio + disabled Next" UX on first paint.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _hasPickedIntent = true;

    public bool IsCreateNew => Intent == WizardIntent.CreateNew;
    public bool IsUseExisting => Intent == WizardIntent.UseExisting;

    public string Step2TitleText =>
        IsCreateNew ? "اسم قاعدة البيانات الجديدة" : "اختر قاعدة البيانات الموجودة";

    // ─── Step 1: server + auth ──────────────────────────────────────────────
    public ObservableCollection<DiscoveredServer> Servers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(ShortServerName))]
    private DiscoveredServer? _selectedServer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(ShortServerName))]
    private string _serverNameInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiscoveryStatusText))]
    private bool _isDiscoveringServers;

    public string DiscoveryStatusText =>
        IsDiscoveringServers
            ? "جاري البحث عن الخوادم…"
            : (Servers.Count > 0 ? $"تم العثور على {Servers.Count} خادم" : "لم يُعثر على خوادم تلقائياً");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWindowsAuth))]
    [NotifyPropertyChangedFor(nameof(IsSqlAuth))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(SummaryAuthMode))]
    private WizardAuthMode _authMode = WizardAuthMode.Windows;

    public bool IsWindowsAuth => AuthMode == WizardAuthMode.Windows;
    public bool IsSqlAuth => AuthMode == WizardAuthMode.SqlAuth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string _username = string.Empty;

    [ObservableProperty] private string _password = string.Empty;

    /// <summary>
    /// اسم الخادم الفعلي: الإدخال اليدوي (إن كان غير فارغ) يتقدّم على اختيار القائمة.
    /// </summary>
    public string EffectiveServer
    {
        get
        {
            var manual = (ServerNameInput ?? string.Empty).Trim();
            if (manual.Length > 0) return manual;
            return SelectedServer?.ConnectionTarget ?? string.Empty;
        }
    }

    /// <summary>اسم خادم مختصر للظهور في DisplayName الافتراضي.</summary>
    public string ShortServerName
    {
        get
        {
            var s = EffectiveServer;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // LocalDB → "LocalDB"
            if (s.IndexOf("localdb", StringComparison.OrdinalIgnoreCase) >= 0) return "LocalDB";
            // host\instance → instance name when available
            var idx = s.IndexOf('\\');
            if (idx >= 0 && idx < s.Length - 1) return s[(idx + 1)..];
            return s;
        }
    }

    // ─── Step 2: database ───────────────────────────────────────────────────
    public ObservableCollection<string> Databases { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(DefaultDisplayNamePreview))]
    private string _databaseName = "NasaqSchoolDb";

    [ObservableProperty] private string? _selectedDatabase;

    [ObservableProperty] private bool _isLoadingDatabases;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDatabaseListError))]
    private string? _databaseListError;

    public bool HasDatabaseListError => !string.IsNullOrEmpty(DatabaseListError);

    partial void OnSelectedDatabaseChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            DatabaseName = value!.Trim();
    }

    // ─── Step 3: test ───────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TestActionButtonText))]
    private bool _isTesting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestResult))]
    [NotifyPropertyChangedFor(nameof(TestActionButtonText))]
    private bool _hasRunTest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TestActionButtonText))]
    private bool _testResultIsSuccess;

    [ObservableProperty] private string? _testResultMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestDetails))]
    private string? _testDetails;

    [ObservableProperty] private bool _showTestDetails;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TestActionButtonText))]
    private bool _testDatabaseExists;

    public bool HasTestDetails => !string.IsNullOrEmpty(TestDetails);
    public bool HasTestResult => HasRunTest && !IsTesting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _canProceedFromTest;

    /// <summary>
    /// نص الزر في الخطوة 3 — يتغيّر بحسب الحالة والنية.
    /// </summary>
    public string TestActionButtonText
    {
        get
        {
            if (!HasRunTest) return "اختبار الاتصال";
            if (IsCreateNew && TestResultIsSuccess && !TestDatabaseExists)
                return "إنشاء قاعدة البيانات";
            return "إعادة الاختبار";
        }
    }

    // ─── Step 4: save ───────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _isFinishing;

    // ─── Step 4: admin account (CreateNew only) ─────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(AdminValidationError))]
    [NotifyPropertyChangedFor(nameof(HasAdminValidationError))]
    private string _adminFullName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(AdminValidationError))]
    [NotifyPropertyChangedFor(nameof(HasAdminValidationError))]
    private string _adminUsername = "admin";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(AdminValidationError))]
    [NotifyPropertyChangedFor(nameof(HasAdminValidationError))]
    private string _adminPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(AdminValidationError))]
    [NotifyPropertyChangedFor(nameof(HasAdminValidationError))]
    private string _adminConfirmPassword = string.Empty;

    /// <summary>
    /// تحقّق مدمج لحقول حساب المدير. يعود null عند الصحة، أو نصاً عربياً
    /// واحداً يصف أول مشكلة. في وضع UseExisting يعود null دائماً (الحقول مخفية).
    /// </summary>
    public string? AdminValidationError
    {
        get
        {
            if (!IsCreateNew) return null;
            if (string.IsNullOrWhiteSpace(AdminFullName)) return "الاسم الكامل مطلوب.";
            if (string.IsNullOrWhiteSpace(AdminUsername)) return "اسم المستخدم مطلوب.";
            if (string.IsNullOrWhiteSpace(AdminPassword)) return "كلمة المرور مطلوبة.";
            if (AdminPassword.Length < 6) return "يجب ألا تقل كلمة المرور عن 6 أحرف.";
            if (AdminPassword != AdminConfirmPassword) return "كلمتا المرور غير متطابقتين.";
            return null;
        }
    }

    public bool HasAdminValidationError => !string.IsNullOrEmpty(AdminValidationError);

    public string DefaultDisplayNamePreview
    {
        get
        {
            var db = (DatabaseName ?? string.Empty).Trim();
            var srv = ShortServerName;
            if (string.IsNullOrWhiteSpace(db) && string.IsNullOrWhiteSpace(srv)) return string.Empty;
            if (string.IsNullOrWhiteSpace(srv)) return db;
            return $"{db} ({srv})";
        }
    }

    // ─── Summary fields (step 4) ────────────────────────────────────────────
    public string SummaryConnectionString
    {
        get
        {
            try { return MaskPassword(BuildConnectionString()); }
            catch { return string.Empty; }
        }
    }

    public string SummaryDataSource
    {
        get
        {
            try { return new SqlConnectionStringBuilder(BuildConnectionString()).DataSource; }
            catch { return string.Empty; }
        }
    }

    public string SummaryDatabase
    {
        get
        {
            try { return new SqlConnectionStringBuilder(BuildConnectionString()).InitialCatalog; }
            catch { return string.Empty; }
        }
    }

    public string SummaryAuthMode => IsWindowsAuth ? "Windows" : "SQL Server (مستخدم وكلمة مرور)";

    // ─── CanGoNext ──────────────────────────────────────────────────────────
    public bool CanGoNext
    {
        get
        {
            switch (CurrentStep)
            {
                case 0:
                    return HasPickedIntent;

                case 1:
                    if (string.IsNullOrWhiteSpace(EffectiveServer)) return false;
                    if (IsSqlAuth && string.IsNullOrWhiteSpace(Username)) return false;
                    return true;

                case 2:
                    return !string.IsNullOrWhiteSpace(DatabaseName);

                case 3:
                    return CanProceedFromTest;

                case 4:
                    if (IsFinishing) return false;
                    if (string.IsNullOrWhiteSpace(DisplayName)) return false;
                    if (IsCreateNew && AdminValidationError != null) return false;
                    return true;

                default:
                    return false;
            }
        }
    }

    // Property changes that don't auto-notify CanGoNext via attributes:
    partial void OnServerNameInputChanged(string value) => OnPropertyChanged(nameof(EffectiveServer));

    // ─── Commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task DiscoverServersAsync(CancellationToken ct)
    {
        if (IsDiscoveringServers) return;
        IsDiscoveringServers = true;
        OnPropertyChanged(nameof(DiscoveryStatusText));
        try
        {
            var found = await _discovery.DiscoverAsync(ct).ConfigureAwait(true);

            Servers.Clear();
            foreach (var s in found) Servers.Add(s);

            // Auto-select the first item (typically LocalDB) if nothing chosen yet.
            if (SelectedServer is null && Servers.Count > 0)
                SelectedServer = Servers[0];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر استكشاف الخوادم", ex.Message, ex);
        }
        finally
        {
            IsDiscoveringServers = false;
            OnPropertyChanged(nameof(DiscoveryStatusText));
        }
    }

    [RelayCommand]
    public async Task LoadDatabasesAsync(CancellationToken ct)
    {
        if (!IsUseExisting) return;
        if (string.IsNullOrWhiteSpace(EffectiveServer)) return;
        if (IsLoadingDatabases) return;

        IsLoadingDatabases = true;
        DatabaseListError = null;
        try
        {
            var masterCs = BuildMasterConnectionString();
            var list = await _tester.ListDatabasesAsync(masterCs, ct).ConfigureAwait(true);

            Databases.Clear();
            foreach (var name in list) Databases.Add(name);

            if (Databases.Count == 0)
            {
                // Try a probe to distinguish "empty server" from "cannot connect".
                var probe = await _tester.TestAsync(masterCs, ct).ConfigureAwait(true);
                if (!probe.Success)
                {
                    DatabaseListError =
                        "تعذّر جلب قائمة القواعد من الخادم. تحقق من بيانات الاعتماد أو أدخل اسم القاعدة يدوياً.";
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            DatabaseListError =
                "تعذّر جلب قائمة القواعد من الخادم. يمكنك إدخال الاسم يدوياً.";
            _errors.Report("تعذّر جلب قائمة قواعد البيانات", ex.Message, ex);
        }
        finally
        {
            IsLoadingDatabases = false;
        }
    }

    [RelayCommand]
    public async Task RunTestActionAsync(CancellationToken ct)
    {
        if (IsTesting) return;

        // Build CS up-front so we can report a clean validation error.
        string cs;
        try
        {
            cs = BuildConnectionString();
        }
        catch (Exception ex)
        {
            TestResultIsSuccess = false;
            TestResultMessage = "تعذّر بناء سلسلة الاتصال. تحقق من الحقول.";
            TestDetails = ex.Message;
            HasRunTest = true;
            CanProceedFromTest = false;
            return;
        }

        IsTesting = true;
        try
        {
            // CreateNew + previous test succeeded + DB missing → user clicked again to create it.
            var shouldCreate =
                IsCreateNew && HasRunTest && TestResultIsSuccess && !TestDatabaseExists;

            if (shouldCreate)
            {
                var createResult = await _tester.CreateDatabaseAsync(cs, ct).ConfigureAwait(true);
                ApplyTestResult(createResult);

                // If creation succeeded, re-test once for an authoritative final state.
                if (createResult.Success && createResult.DatabaseExists)
                {
                    var retest = await _tester.TestAsync(cs, ct).ConfigureAwait(true);
                    ApplyTestResult(retest);
                }
                return;
            }

            var result = await _tester.TestAsync(cs, ct).ConfigureAwait(true);
            ApplyTestResult(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر اختبار الاتصال", ex.Message, ex);
        }
        finally
        {
            IsTesting = false;
            OnPropertyChanged(nameof(HasTestResult));
            OnPropertyChanged(nameof(TestActionButtonText));
        }
    }

    private void ApplyTestResult(ConnectionTestResult result)
    {
        TestResultIsSuccess = result.Success;
        TestResultMessage = result.Message;
        TestDetails = result.Details;
        TestDatabaseExists = result.DatabaseExists;
        HasRunTest = true;

        // Next is enabled the moment the DB exists and we connected successfully.
        CanProceedFromTest = result.Success && result.DatabaseExists;
    }

    [RelayCommand]
    private void ToggleDetails() => ShowTestDetails = !ShowTestDetails;

    [RelayCommand]
    private async Task NextStepAsync(CancellationToken ct)
    {
        if (!CanGoNext) return;

        if (IsLastStep)
        {
            await FinishAsync().ConfigureAwait(true);
            return;
        }

        var current = CurrentStep;
        CurrentStep = current + 1;

        // ─── Step entry side effects ────────────────────────────────────────
        try
        {
            if (CurrentStep == 1 && Servers.Count == 0)
            {
                // Discover servers when entering the server step for the first time.
                await DiscoverServersAsync(ct).ConfigureAwait(true);
            }
            else if (CurrentStep == 2)
            {
                if (IsUseExisting)
                {
                    await LoadDatabasesAsync(ct).ConfigureAwait(true);
                }
            }
            else if (CurrentStep == 3)
            {
                // Re-test from scratch when the user reaches the test step.
                ResetTestState();
            }
            else if (CurrentStep == 4)
            {
                // Default DisplayName when empty so the user can hit Finish quickly.
                if (string.IsNullOrWhiteSpace(DisplayName))
                    DisplayName = DefaultDisplayNamePreview;
            }
        }
        catch (OperationCanceledException) { /* swallow — navigation continues */ }
    }

    [RelayCommand]
    private void PrevStep()
    {
        if (CurrentStep > 0) CurrentStep--;
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (IsFinishing) return;
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            // Belt-and-suspenders — CanGoNext already gates this.
            return;
        }

        IsFinishing = true;
        try
        {
            var cs = BuildConnectionString();

            // وضع CreateNew: قبل تسجيل الاتصال، احفظ بيانات المدير المؤقتة
            // ليستهلكها DbSeeder عند أول تشغيل بعد إعادة التشغيل. إن فشلت
            // الكتابة لا نتقدّم — لأن القاعدة ستُنشأ بمدير افتراضي غير مرغوب.
            if (IsCreateNew)
            {
                try
                {
                    _pendingAdminStore.Save(new PendingAdminSetup
                    {
                        FullName = (AdminFullName ?? string.Empty).Trim(),
                        Username = (AdminUsername ?? string.Empty).Trim(),
                        Password = AdminPassword ?? string.Empty
                    });
                }
                catch (Exception ex)
                {
                    TestResultMessage = "تعذّر حفظ بيانات المدير المؤقتة. لم تُحفظ إعدادات الاتصال.";
                    _errors.Report("تعذّر حفظ بيانات المدير المؤقتة", ex.Message, ex);
                    return;
                }
            }

            try
            {
                var entry = _registry.Add(DisplayName.Trim(), cs);
                _registry.SetActive(entry.Id);
            }
            catch
            {
                // فشل تسجيل الاتصال بعد حفظ بيانات المدير → نظّف الملف المؤقت
                // لئلا يستهلكه DbSeeder لاحقاً على قاعدة بيانات لا علاقة لها.
                if (IsCreateNew)
                {
                    try { _pendingAdminStore.ReadAndClear(); } catch { /* أفضل جهد */ }
                }
                throw;
            }

            RequestClose?.Invoke(true);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حفظ إعدادات الاتصال", ex.Message, ex);
        }
        finally
        {
            IsFinishing = false;
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// يبني سلسلة الاتصال النهائية من الحقول الحالية وبإعدادات افتراضية تتطابق
    /// مع <c>appsettings.json</c> (TrustServerCertificate, MultipleActiveResultSets).
    /// </summary>
    public string BuildConnectionString()
    {
        var dbName = (DatabaseName ?? string.Empty).Trim();
        if (dbName.Length == 0) dbName = "NasaqSchoolDb";

        var b = new SqlConnectionStringBuilder
        {
            InitialCatalog = dbName,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = 15,
            DataSource = string.IsNullOrWhiteSpace(EffectiveServer) ? "." : EffectiveServer
        };

        if (IsWindowsAuth)
        {
            b.IntegratedSecurity = true;
        }
        else
        {
            b.IntegratedSecurity = false;
            b.UserID = (Username ?? string.Empty).Trim();
            b.Password = Password ?? string.Empty;
        }

        return b.ConnectionString;
    }

    private string BuildMasterConnectionString()
    {
        var b = new SqlConnectionStringBuilder(BuildConnectionString())
        {
            InitialCatalog = "master",
            Pooling = false,
            ConnectTimeout = 8
        };
        return b.ConnectionString;
    }

    private void ResetTestState()
    {
        TestResultMessage = null;
        TestDetails = null;
        ShowTestDetails = false;
        CanProceedFromTest = false;
        HasRunTest = false;
        TestResultIsSuccess = false;
        TestDatabaseExists = false;
        OnPropertyChanged(nameof(HasTestResult));
        OnPropertyChanged(nameof(TestActionButtonText));
    }

    private static string MaskPassword(string connectionString)
    {
        try
        {
            var b = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(b.Password))
                b.Password = "***";
            return b.ConnectionString;
        }
        catch
        {
            return connectionString;
        }
    }
}
