using System.Windows;
using System.Windows.Input;

namespace Nasag.Views.Splash;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void OnBodyMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* DragMove can throw if the mouse state changed mid-drag — safe to ignore. */ }
        }
    }
}
