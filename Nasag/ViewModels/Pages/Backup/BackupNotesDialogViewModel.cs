using CommunityToolkit.Mvvm.ComponentModel;

namespace Nasag.ViewModels.Pages.Backup;

/// <summary>
/// Tiny VM exposing a single editable Notes string for the "create backup"
/// dialog. Kept as a real VM (rather than ad-hoc fields on the dialog) so the
/// future "edit-notes" or test paths can re-use it cleanly.
/// </summary>
public sealed partial class BackupNotesDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _notes;
}
