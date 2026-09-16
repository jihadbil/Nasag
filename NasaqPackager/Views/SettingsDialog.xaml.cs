using System.Windows;
using System.Windows.Input;
using NasaqPackager.ViewModels;

namespace NasaqPackager.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SettingsDialogViewModel oldVm)
            oldVm.RequestClose -= HandleRequestClose;

        if (e.NewValue is SettingsDialogViewModel newVm)
            newVm.RequestClose += HandleRequestClose;
    }

    private void HandleRequestClose()
    {
        if (DataContext is SettingsDialogViewModel vm)
            DialogResult = vm.DialogResult;
        Close();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); } catch { }
        }
    }
}
