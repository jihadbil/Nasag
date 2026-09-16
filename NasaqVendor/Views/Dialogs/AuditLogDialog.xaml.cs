using System.Windows;
using System.Windows.Input;
using NasaqVendor.ViewModels.Dialogs;

namespace NasaqVendor.Views.Dialogs;

public partial class AuditLogDialog : Window
{
    public AuditLogDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AuditLogViewModel vm)
                vm.RequestClose += (_, _) => Close();
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
