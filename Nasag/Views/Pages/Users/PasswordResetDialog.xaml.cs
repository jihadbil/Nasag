using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nasag.ViewModels.Pages.Users;

namespace Nasag.Views.Pages.Users;

public partial class PasswordResetDialog : Window
{
    private PasswordResetDialogViewModel? _vm;
    private bool _syncing;

    public PasswordResetDialog()
    {
        InitializeComponent();
    }

    public static bool Show(PasswordResetDialogViewModel vm)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new PasswordResetDialog
        {
            DataContext = vm,
            Owner = owner
        };
        dlg._vm = vm;

        var saved = false;
        void onSaved(object? _, System.EventArgs __) { saved = true; dlg.Close(); }
        void onCancelled(object? _, System.EventArgs __) => dlg.Close();
        vm.Saved += onSaved;
        vm.Cancelled += onCancelled;
        try
        {
            dlg.Loaded += (_, _) => dlg.NewPasswordBox.Focus();
            dlg.ShowDialog();
        }
        finally
        {
            vm.Saved -= onSaved;
            vm.Cancelled -= onCancelled;
        }
        return saved;
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore */ }
        }
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _syncing || sender is not PasswordBox box) return;
        _vm.NewPassword = box.Password;
        // Keep the visible TextBox in sync so toggling "show" reveals the same value.
        if (NewPasswordPlain.Text != box.Password)
        {
            _syncing = true;
            NewPasswordPlain.Text = box.Password;
            _syncing = false;
        }
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _syncing || sender is not PasswordBox box) return;
        _vm.ConfirmPassword = box.Password;
        if (ConfirmPasswordPlain.Text != box.Password)
        {
            _syncing = true;
            ConfirmPasswordPlain.Text = box.Password;
            _syncing = false;
        }
    }

    private void OnNewPlainChanged(object sender, TextChangedEventArgs e)
    {
        if (_vm is null || _syncing) return;
        _vm.NewPassword = NewPasswordPlain.Text;
        if (NewPasswordBox.Password != NewPasswordPlain.Text)
        {
            _syncing = true;
            NewPasswordBox.Password = NewPasswordPlain.Text;
            _syncing = false;
        }
    }

    private void OnConfirmPlainChanged(object sender, TextChangedEventArgs e)
    {
        if (_vm is null || _syncing) return;
        _vm.ConfirmPassword = ConfirmPasswordPlain.Text;
        if (ConfirmPasswordBox.Password != ConfirmPasswordPlain.Text)
        {
            _syncing = true;
            ConfirmPasswordBox.Password = ConfirmPasswordPlain.Text;
            _syncing = false;
        }
    }

    private void OnShowToggleChanged(object sender, RoutedEventArgs e)
    {
        if (ShowToggle.IsChecked == true)
        {
            NewPasswordBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            NewPasswordPlain.Visibility = Visibility.Visible;
            ConfirmPasswordPlain.Visibility = Visibility.Visible;
        }
        else
        {
            NewPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
            NewPasswordPlain.Visibility = Visibility.Collapsed;
            ConfirmPasswordPlain.Visibility = Visibility.Collapsed;
        }
    }
}
