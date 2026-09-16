using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nasag.ViewModels.Pages.Users;

namespace Nasag.Views.Pages.Users;

public partial class UserEditorDialog : Window
{
    private UserEditorDialogViewModel? _vm;

    public UserEditorDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the dialog for the given VM and returns true when the user saved
    /// successfully (the VM raised <see cref="UserEditorDialogViewModel.Saved"/>).
    /// </summary>
    public static bool Show(UserEditorDialogViewModel vm)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new UserEditorDialog
        {
            DataContext = vm,
            Owner = owner
        };
        dlg.Bind(vm);
        var saved = false;
        void onSaved(object? _, System.EventArgs __) { saved = true; dlg.Close(); }
        void onCancelled(object? _, System.EventArgs __) => dlg.Close();
        vm.Saved += onSaved;
        vm.Cancelled += onCancelled;
        try { dlg.ShowDialog(); }
        finally
        {
            vm.Saved -= onSaved;
            vm.Cancelled -= onCancelled;
        }
        return saved;
    }

    private void Bind(UserEditorDialogViewModel vm)
    {
        _vm = vm;
        // Seed PasswordBoxes from VM (used in Edit mode the password fields are hidden;
        // in Add mode we want them to start empty regardless).
        NewPasswordBox.Password = vm.NewPassword;
        ConfirmPasswordBox.Password = vm.ConfirmPassword;
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || sender is not PasswordBox box) return;
        // PasswordBox can't TwoWay-bind for security reasons, so push the
        // current value into the VM on each change instead.
        _vm.NewPassword = box.Password;
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || sender is not PasswordBox box) return;
        _vm.ConfirmPassword = box.Password;
    }
}
