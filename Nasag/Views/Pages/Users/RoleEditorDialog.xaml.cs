using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.ViewModels.Pages.Users;

namespace Nasag.Views.Pages.Users;

public partial class RoleEditorDialog : Window
{
    public RoleEditorDialog()
    {
        InitializeComponent();
    }

    public static bool Show(RoleEditorDialogViewModel vm)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new RoleEditorDialog
        {
            DataContext = vm,
            Owner = owner
        };

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

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }
}
