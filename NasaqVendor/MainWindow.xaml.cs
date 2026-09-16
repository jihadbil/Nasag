using System.Windows;
using System.Windows.Input;
using NasaqVendor.Services;
using NasaqVendor.ViewModels;

namespace NasaqVendor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    public Views.Common.ToastHost GetToastHost() => ToastHostInstance;
}
