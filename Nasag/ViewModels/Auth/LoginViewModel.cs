using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Nasag.Services;
using Nasag.ViewModels.Setup;
using Nasag.Views.Setup;

namespace Nasag.ViewModels.Auth;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserPreferencesService _prefs;
    private readonly IConnectionRegistry _registry;
    private readonly IApplicationRestarter _restarter;
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogs;
    private readonly IErrorReporter _errors;
    private readonly IToastService _toasts;

    // Guards re-entry while we revert SelectedConnection programmatically (after
    // the user cancels the restart prompt) so OnSelectedConnectionChanged doesn't
    // re-prompt in a loop.
    private bool _isSwitchingProgrammatically;

    public LoginViewModel(
        IAuthService auth,
        ICurrentUserService currentUser,
        IUserPreferencesService prefs,
        IConnectionRegistry registry,
        IApplicationRestarter restarter,
        IServiceProvider services,
        IDialogService dialogs,
        IErrorReporter errors,
        IToastService toasts)
    {
        _auth = auth;
        _currentUser = currentUser;
        _prefs = prefs;
        _registry = registry;
        _restarter = restarter;
        _services = services;
        _dialogs = dialogs;
        _errors = errors;
        _toasts = toasts;

        // Hydrate Remember Me from local preferences on construction so that
        // the username + password are pre-filled when the login window appears.
        // كلمة المرور تأتي مُشفَّرة بـ DPAPI ضمن نفس الـ prefs.json.
        var remembered = _prefs.Current.RememberedUsername;
        if (!string.IsNullOrWhiteSpace(remembered))
        {
            _username = remembered;
            _rememberMe = true;
            var rememberedPwd = _prefs.Current.GetRememberedPassword();
            if (!string.IsNullOrEmpty(rememberedPwd))
                _password = rememberedPwd;
        }

        // Assign backing field directly so OnSelectedConnectionChanged doesn't fire
        // during construction (AI_INSTRUCTIONS section 13 — no setter cascades in ctor).
        _selectedConnection = _registry.Active;

        // Note: LoginViewModel is registered as Transient. Each Login window resolves
        // a fresh instance, so we accept that the strong subscription below "leaks"
        // for the (short) lifetime between login window display and successful sign-in.
        // A weak event would be over-engineering here.
        _registry.Changed += OnRegistryChanged;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private SavedConnection? _selectedConnection;

    public string AppName => "نَسَق لإدارة المدارس";
    public string AppTagline => "كل بيانات المدرسة في نظام واحد بسيط";

    /// <summary>
    /// السجل العام — مكشوف للواجهة لتربط على <c>Registry.IsEmpty</c> أو سمات أخرى.
    /// </summary>
    public IConnectionRegistry Registry => _registry;

    /// <summary>
    /// قائمة الاتصالات المتاحة في السجل (لقطة طازجة في كل وصول).
    /// </summary>
    public IReadOnlyList<SavedConnection> AvailableConnections => _registry.All;

    private void OnRegistryChanged(object? sender, EventArgs e)
    {
        // Marshal to UI thread (Changed may fire from any thread).
        var app = Application.Current;
        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(RefreshRegistryProjections);
            return;
        }
        RefreshRegistryProjections();
    }

    private void RefreshRegistryProjections()
    {
        OnPropertyChanged(nameof(AvailableConnections));
        OnPropertyChanged(nameof(Registry));

        // Keep the picker in sync with the active connection without retriggering the
        // confirm-and-restart dance.
        var active = _registry.Active;
        if (!ReferenceEquals(SelectedConnection, active)
            && (SelectedConnection?.Id != active?.Id))
        {
            _isSwitchingProgrammatically = true;
            try { SelectedConnection = active; }
            finally { _isSwitchingProgrammatically = false; }
        }
    }

    partial void OnSelectedConnectionChanged(SavedConnection? oldValue, SavedConnection? newValue)
    {
        if (_isSwitchingProgrammatically) return;
        if (newValue is null) return;

        var active = _registry.Active;
        if (active is not null && newValue.Id == active.Id) return;

        // Fire-and-forget: the async path will confirm with the user, then either
        // restart or revert the selection.
        _ = SwitchConnectionAsync(newValue);
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            var result = await _auth.SignInAsync(Username.Trim(), Password).ConfigureAwait(true);

            if (result.IsSuccess && result.User is not null)
            {
                // Persist (or forget) the username + password based on the checkbox state.
                // كلمة المرور تُشفَّر بـ DPAPI (per-user) داخل prefs.json.
                if (RememberMe)
                {
                    _prefs.Current.RememberedUsername = Username.Trim();
                    _prefs.Current.SetRememberedPassword(Password);
                }
                else
                {
                    _prefs.Current.RememberedUsername = null;
                    _prefs.Current.SetRememberedPassword(null);
                }
                _prefs.Save();

                _currentUser.SignIn(result.User);
                return;
            }

            ShowError(result.ErrorMessage ?? "تعذّر تسجيل الدخول.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// يطلب تأكيداً من المستخدم، ثم يجعل <paramref name="target"/> هو الاتصال النشط
    /// ويعيد تشغيل البرنامج. في حالة الرفض يُعيد <see cref="SelectedConnection"/>
    /// إلى الاتصال النشط السابق دون الدخول في حلقة إشعار.
    /// </summary>
    [RelayCommand]
    private async Task SwitchConnectionAsync(SavedConnection target)
    {
        if (target is null) return;

        var previousActive = _registry.Active;
        if (previousActive is not null && previousActive.Id == target.Id) return;

        try
        {
            var ok = await _dialogs.ConfirmAsync(
                "تبديل قاعدة البيانات",
                $"سيتم تبديل قاعدة البيانات إلى «{target.DisplayName}». سيُعاد تشغيل البرنامج لتطبيق التغيير. هل تريد المتابعة؟")
                .ConfigureAwait(true);

            if (!ok)
            {
                // Revert the picker without retriggering this method.
                _isSwitchingProgrammatically = true;
                try { SelectedConnection = previousActive; }
                finally { _isSwitchingProgrammatically = false; }
                return;
            }

            _registry.SetActive(target.Id);
            _restarter.RestartNow();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر تبديل قاعدة البيانات", ex.Message, ex);

            _isSwitchingProgrammatically = true;
            try { SelectedConnection = previousActive; }
            finally { _isSwitchingProgrammatically = false; }
        }
    }

    /// <summary>
    /// يفتح معالج الإعداد لإضافة اتصال جديد. إن نجح المعالج (Save → DialogResult=true)
    /// يكون قد أضاف الاتصال وعيّنه نشطاً، فنعيد تشغيل البرنامج فوراً بدون تأكيد إضافي
    /// (المستخدم قد قرر فعلاً من داخل المعالج).
    /// </summary>
    [RelayCommand]
    private void AddConnection()
    {
        try
        {
            var wizard = _services.GetRequiredService<SetupWizardWindow>();
            wizard.DataContext = _services.GetRequiredService<SetupWizardViewModel>();
            wizard.Owner = Application.Current?.MainWindow;

            var result = wizard.ShowDialog();
            if (result == true)
            {
                _restarter.RestartNow();
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح معالج إضافة قاعدة البيانات", ex.Message, ex);
        }
    }

    private bool CanLogin() => !IsBusy
                               && !string.IsNullOrWhiteSpace(Username)
                               && !string.IsNullOrEmpty(Password);

    partial void OnUsernameChanged(string value)
    {
        ClearError();
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        ClearError();
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = null;
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
