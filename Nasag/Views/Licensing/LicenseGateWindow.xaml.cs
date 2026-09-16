using System.Windows;
using System.Windows.Input;

namespace Nasag.Views.Licensing;

public partial class LicenseGateWindow : Window
{
    public LicenseGateWindow()
    {
        InitializeComponent();
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click race */ }
        }
    }
}
