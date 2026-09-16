using System.Windows;
using System.Windows.Input;

namespace Nasag.Views.Pages.Reports;

public partial class FeesReportDialog : Window
{
    public FeesReportDialog()
    {
        InitializeComponent();
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore */ }
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
