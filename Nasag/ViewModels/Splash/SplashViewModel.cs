using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Data;
using Nasag.Services;

namespace Nasag.ViewModels.Splash;

/// <summary>
/// Result delivered by the SplashViewModel when database initialization finishes
/// (either successfully or with an error). Consumed by <see cref="App"/> to decide
/// which window to show next.
/// </summary>
public sealed record SplashResult(DatabaseInitStatus Status, string? ErrorMessage);

public partial class SplashViewModel : ObservableObject
{
    private readonly IDatabaseInitializer _initializer;
    private readonly IConnectionRegistry _registry;
    private readonly IErrorReporter _errors;

    public SplashViewModel(
        IDatabaseInitializer initializer,
        IConnectionRegistry registry,
        IErrorReporter errors)
    {
        _initializer = initializer;
        _registry = registry;
        _errors = errors;
    }

    [ObservableProperty]
    private string _statusMessage = "جاري التحضير…";

    [ObservableProperty]
    private bool _isWorking = true;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private DatabaseInitStatus _failureStatus = DatabaseInitStatus.Success;

    /// <summary>
    /// Fired when initialization concludes — caller decides what window to show next.
    /// </summary>
    public event EventHandler<SplashResult>? Completed;

    [RelayCommand]
    public async Task RunInitAsync(CancellationToken ct = default)
    {
        SetUiState(working: true, errorText: null, status: DatabaseInitStatus.Success);
        SetStatus("جاري التحقق من الاتصال بقاعدة البيانات…");

        DatabaseInitResult result;
        try
        {
            // First — probe pending migrations on a background task so we can flip the
            // status to "Updating database…" if any are about to be applied.
            // The initializer itself is monolithic, so we just update status optimistically.
            await Task.Delay(80, ct).ConfigureAwait(true);
            SetStatus("جاري التحقق من التحديثات…");

            // Run the actual initializer off the UI thread.
            result = await Task.Run(async () =>
            {
                // Optimistically flip status to "updating database…"; the initializer will
                // run the migration regardless. If there are none, this flash is harmless.
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (!ct.IsCancellationRequested)
                        SetStatus("جاري تحديث قاعدة البيانات…");
                });

                return await _initializer.InitializeAsync(ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _errors.Report("فشل بدء التشغيل", ex.Message, ex);
            HandleFailure(DatabaseInitStatus.Unknown, "حدث خطأ غير متوقع أثناء تهيئة قاعدة البيانات.");
            return;
        }

        if (result.IsSuccess)
        {
            SetStatus("جاري تحميل البيانات الأولية…");
            await Task.Delay(120, ct).ConfigureAwait(true);

            // أول تشغيل بعد Migration من Phase 13 القديم قد يترك السجل فارغاً
            // والاتصال يأتي من appsettings.json — في هذه الحالة فقط نُضيف إدخالاً
            // للسجل ليظهر اسم في شاشة الدخول.
            // مهم: إن كان المصدر "Default" (LocalDB sentinel — لا appsettings ولا
            // user override) فهذا يعني تثبيت جديد بدون قاعدة فعلية؛ لا نُحرّك
            // السجل لئلا نخفي معالج الإعداد عن المستخدم في التشغيلات اللاحقة.
            try
            {
                if (_registry.IsEmpty && _registry.Source != "Default")
                    _registry.Add("قاعدة البيانات الافتراضية", _registry.ActiveConnectionString);

                if (!_registry.IsEmpty)
                    _registry.MarkActiveUsed();
            }
            catch (Exception ex)
            {
                // لا نُفشل بدء التشغيل بسبب فشل كتابة سجل الاتصالات.
                _errors.Report("تعذّر تحديث سجل الاتصالات", ex.Message, ex);
            }

            SetStatus("جاهز");
            IsWorking = false;
            Completed?.Invoke(this, new SplashResult(DatabaseInitStatus.Success, null));
            return;
        }

        if (result.Status == DatabaseInitStatus.CannotConnect)
        {
            // Don't show error state — App will open the Setup Wizard instead.
            IsWorking = false;
            Completed?.Invoke(this, new SplashResult(result.Status, result.ErrorMessage));
            return;
        }

        // Terminal error (migration/seed/unknown) — render error UI and let user retry.
        var message = string.IsNullOrWhiteSpace(result.Details)
            ? result.ErrorMessage ?? "حدث خطأ غير متوقع."
            : $"{result.ErrorMessage}\n\nالتفاصيل: {result.Details}";
        HandleFailure(result.Status, message);
    }

    [RelayCommand]
    public async Task RetryAsync(CancellationToken ct = default)
    {
        SetUiState(working: true, errorText: null, status: DatabaseInitStatus.Success);
        await RunInitAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    public void OpenSetupWizard()
    {
        // Force the App to show the wizard by signalling CannotConnect.
        IsWorking = false;
        Completed?.Invoke(this, new SplashResult(DatabaseInitStatus.CannotConnect, ErrorMessage));
    }

    private void SetStatus(string text)
    {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
            Application.Current.Dispatcher.Invoke(() => StatusMessage = text);
        else
            StatusMessage = text;
    }

    private void HandleFailure(DatabaseInitStatus status, string message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            FailureStatus = status;
            ErrorMessage = message;
            HasError = true;
            IsWorking = false;
            StatusMessage = "تعذّر بدء التشغيل";
        });
        Completed?.Invoke(this, new SplashResult(status, message));
    }

    private void SetUiState(bool working, string? errorText, DatabaseInitStatus status)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsWorking = working;
            HasError = !string.IsNullOrEmpty(errorText);
            ErrorMessage = errorText ?? string.Empty;
            FailureStatus = status;
        });
    }
}
