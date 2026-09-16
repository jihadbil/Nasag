using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace Nasag.Services.Printing;

/// <summary>
/// Lightweight wrapper around WPF's built-in PrintDialog/DocumentPaginator pipeline.
/// Provides a preview-then-print flow via <see cref="PreviewAndPrint"/> and a direct
/// print flow via <see cref="Print"/>. No external dependencies.
/// </summary>
public static class PrintService
{
    public static void PreviewAndPrint(FlowDocument document, string description)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var preview = new Nasag.Views.Common.PrintPreviewWindow(document, description) { Owner = owner };
        preview.ShowDialog();
    }

    public static bool Print(FlowDocument document, string description)
    {
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return false;
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
        dlg.PrintDocument(paginator, description);
        return true;
    }
}
