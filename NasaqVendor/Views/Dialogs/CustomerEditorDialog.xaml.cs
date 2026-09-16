using System.Windows;
using System.Windows.Input;
using NasaqVendor.ViewModels.Dialogs;

namespace NasaqVendor.Views.Dialogs;

public partial class CustomerEditorDialog : Window
{
    public CustomerEditorDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is CustomerEditorViewModel vm)
                vm.RequestClose += (_, ok) => { DialogResult = ok; Close(); };
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
