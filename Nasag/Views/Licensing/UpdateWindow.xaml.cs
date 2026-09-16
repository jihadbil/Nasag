using System.Windows;
using System.Windows.Input;
using Nasag.ViewModels.Licensing;

namespace Nasag.Views.Licensing;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is UpdateViewModel vm)
        {
            vm.RequestClose = () =>
            {
                try { Close(); } catch { /* ignore */ }
            };
        }
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UpdateViewModel vm)
        {
            await vm.CheckAsync();
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
