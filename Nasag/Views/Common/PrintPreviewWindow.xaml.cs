using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Nasag.Services.Printing;

namespace Nasag.Views.Common;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow(FlowDocument document, string description)
    {
        InitializeComponent();
        Title = description;
        TitleLabel.Text = description;

        // A4 at 96 DPI — only apply defaults when the FlowDocument hasn't set its own dimensions
        // (so landscape reports keep their own page size).
        if (double.IsNaN(document.PageWidth)) document.PageWidth = 793;
        if (double.IsNaN(document.PageHeight)) document.PageHeight = 1122;
        if (document.ColumnWidth == 0 || double.IsNaN(document.ColumnWidth)) document.ColumnWidth = document.PageWidth;
        if (document.PagePadding == default) document.PagePadding = new Thickness(48);
        Viewer.Document = document;

        // Hide the FlowDocumentReader's built-in toolbar after the template applies — we have our own top bar.
        Viewer.Loaded += (_, _) =>
        {
            if (FindVisualChild<ToolBar>(Viewer) is { } toolBar)
                toolBar.Visibility = Visibility.Collapsed;
        };

        // Allow dragging the window from the top bar area since we removed system chrome.
        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { /* DragMove can throw if not in the right state — ignore. */ }
            }
        };
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (Viewer.Document is FlowDocument doc)
            PrintService.Print(doc, Title ?? "Document");
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            if (FindVisualChild<T>(child) is { } nested) return nested;
        }
        return null;
    }
}
