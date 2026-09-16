using System.Windows;
using System.Windows.Input;
using NasaqVendor.ViewModels.Dialogs;

namespace NasaqVendor.Views.Dialogs;

public partial class IssueLicenseDialog : Window
{
    public IssueLicenseDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is IssueLicenseViewModel vm)
                vm.RequestClose += (_, ok) => { DialogResult = ok; Close(); };
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
