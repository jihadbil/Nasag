using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.ViewModels.Pages.Backup;

namespace Nasag.Views.Pages.Backup;

/// <summary>
/// Borderless dialog asking the operator for an optional Notes string before
/// kicking off a backup. Returns <c>null</c> from <see cref="Show"/> when the
/// user cancels; returns the (possibly empty) string otherwise.
/// </summary>
public partial class BackupNotesDialog : Window
{
    private readonly BackupNotesDialogViewModel _vm = new();

    /// <summary>Confirmed notes payload — null when the user cancelled.</summary>
    public string? Result { get; private set; }

    private BackupNotesDialog()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += (_, _) =>
        {
            NotesBox.Focus();
            NotesBox.CaretIndex = NotesBox.Text?.Length ?? 0;
        };
    }

    public static string? Show(Window? owner = null)
    {
        var resolvedOwner = owner ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                                  ?? Application.Current?.MainWindow;
        var dlg = new BackupNotesDialog { Owner = resolvedOwner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        // Empty string is a valid "no notes" — return string.Empty rather than
        // null so the caller can distinguish confirmed-without-notes from cancel.
        var text = NotesBox.Text?.Trim();
        Result = string.IsNullOrEmpty(text) ? string.Empty : text;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}
