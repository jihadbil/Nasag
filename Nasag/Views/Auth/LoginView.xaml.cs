using System.Windows;
using System.Windows.Controls;
using Nasag.ViewModels.Auth;

namespace Nasag.Views.Auth;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object sender, RoutedEventArgs e)
    {
        // إن كان المستخدم محفوظاً عبر «تذكّرني»، أعِد ملء PasswordBox من الـ VM
        // (لا يمكن ربط Password مباشرة لأسباب أمنية في WPF) — ثم انقل التركيز
        // مباشرة لزر تسجيل الدخول حتى يكفي Enter للدخول دون إعادة الكتابة.
        if (DataContext is LoginViewModel vm && !string.IsNullOrEmpty(vm.Password))
        {
            PasswordField.Password = vm.Password;
            PasswordField.Focus();
        }
        else
        {
            UsernameField.Focus();
        }
    }

    /// <summary>
    /// WPF's <see cref="PasswordBox"/> doesn't expose the password as a bindable DP for security reasons;
    /// pipe it to the view model imperatively instead.
    /// </summary>
    private void PasswordField_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }
}
