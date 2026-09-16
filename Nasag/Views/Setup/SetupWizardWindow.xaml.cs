using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nasag.ViewModels.Setup;

namespace Nasag.Views.Setup;

/// <summary>
/// النافذة المضيفة لمعالج الإعداد ذي الخمس خطوات. الـ code-behind محدود بـ:
///   (أ) أحداث الإطار البلا حواف (Drag region).
///   (ب) جسر RequestClose ← DialogResult.
///   (ج) جسر PasswordBox ← الـ VM (PasswordBox لا تكشف Password كـ DP).
///   (د) معالجات الـ Click على بطاقات الاختيار (Intent/Auth) لأنّ ربط
///       <c>IsChecked</c> ثنائي الاتجاه بـ bool مشتق من enum ليس مرناً.
///   (هـ) إطلاق DiscoverServersAsync بشكل غير حاجب عند تحميل النافذة لتحضير
///       قائمة الخوادم قبل وصول المستخدم إلى الخطوة 1.
/// كل منطق الأعمال يعيش في <see cref="SetupWizardViewModel"/>.
/// </summary>
public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SetupWizardViewModel vm)
        {
            vm.RequestClose = result =>
            {
                try { DialogResult = result; }
                catch { /* not shown as dialog — ignore */ }
                Close();
            };
        }
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Warm up the server list while the user reads the welcome step so the
        // dropdown is populated by the time they reach step 1.
        if (DataContext is SetupWizardViewModel vm && vm.Servers.Count == 0)
        {
            try
            {
                await vm.DiscoverServersAsync(CancellationToken.None).ConfigureAwait(true);
            }
            catch
            {
                // VM already reports through IErrorReporter; nothing to do here.
            }
        }
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    // ─── Step 0 intent cards ────────────────────────────────────────────────

    private void OnIntentCreateNewClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
        {
            vm.Intent = WizardIntent.CreateNew;
            vm.HasPickedIntent = true;
        }
    }

    private void OnIntentUseExistingClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
        {
            vm.Intent = WizardIntent.UseExisting;
            vm.HasPickedIntent = true;
        }
    }

    // ─── Step 1 auth-mode radios ────────────────────────────────────────────

    private void OnAuthWindowsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm) vm.AuthMode = WizardAuthMode.Windows;
    }

    private void OnAuthSqlClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm) vm.AuthMode = WizardAuthMode.SqlAuth;
    }

    /// <summary>
    /// Pushes <see cref="PasswordBox.Password"/> into the view-model when it
    /// changes — WPF's PasswordBox intentionally doesn't expose Password as a DP.
    /// </summary>
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    /// <summary>
    /// Restores a previously-typed password into the field when Step 1 is shown
    /// again (Back → Next navigation). Without this, going back hides the field,
    /// and re-entering shows it empty even though the VM still holds the value.
    /// </summary>
    private void RestorePasswordFromVm()
    {
        if (DataContext is SetupWizardViewModel vm && PasswordField is not null
            && PasswordField.Password != vm.Password)
        {
            PasswordField.Password = vm.Password ?? string.Empty;
        }
    }

    private void OnPasswordFieldLoaded(object sender, RoutedEventArgs e)
        => RestorePasswordFromVm();

    private void OnPasswordFieldVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool nowVisible && nowVisible) RestorePasswordFromVm();
    }

    // ─── Step 4 admin password bridges ──────────────────────────────────────

    /// <summary>
    /// يدفع كلمة مرور المدير من PasswordBox إلى الـ VM (PasswordBox.Password
    /// ليست DependencyProperty قابلة للربط).
    /// </summary>
    private void OnAdminPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb)
            vm.AdminPassword = pb.Password;
    }

    private void OnAdminConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb)
            vm.AdminConfirmPassword = pb.Password;
    }

    /// <summary>
    /// يعيد ملء PasswordBox عند ظهور الخطوة 4 مجدداً (Back → Next) إذا كانت
    /// قيمة الـ VM غير فارغة.
    /// </summary>
    private void OnAdminPasswordLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb
            && !string.IsNullOrEmpty(vm.AdminPassword) && pb.Password != vm.AdminPassword)
        {
            pb.Password = vm.AdminPassword;
        }
    }

    private void OnAdminConfirmPasswordLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb
            && !string.IsNullOrEmpty(vm.AdminConfirmPassword) && pb.Password != vm.AdminConfirmPassword)
        {
            pb.Password = vm.AdminConfirmPassword;
        }
    }
}
