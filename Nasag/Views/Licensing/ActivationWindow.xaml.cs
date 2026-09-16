using System.Windows;
using System.Windows.Input;
using Nasag.ViewModels.Licensing;

namespace Nasag.Views.Licensing;

public partial class ActivationWindow : Window
{
    public ActivationWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ActivationViewModel vm)
        {
            vm.RequestClose = result =>
            {
                try { DialogResult = result; }
                catch { /* not shown as dialog — ignore */ }
                Close();
            };
        }
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore */ }
        }
    }
}
