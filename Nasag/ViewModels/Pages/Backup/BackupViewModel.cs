using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Nasag.Models;
using Nasag.Services;
using Nasag.Views.Common;
using Nasag.Views.Pages.Backup;

namespace Nasag.ViewModels.Pages.Backup;

/// <summary>
/// Page VM for the Backup &amp; Restore screen. Coordinates the BackupService
/// (BACKUP / RESTORE) with the BackupsRepository (audit log) and surfaces a
/// list of past operations.
/// </summary>
public sealed partial class BackupViewModel : PageViewModel
{
    private readonly IBackupService _backupService;
    private readonly IBackupsRepository _backupsRepo;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly IBusyService _busy;
    private readonly ICurrentUserService _currentUser;
    private readonly IConnectionMonitor _connection;
    private readonly IUserPreferencesService _prefs;

    private bool _isInitializing = true;
    private bool _reloadInFlight;

    public BackupViewModel(
        IBackupService backupService,
        IBackupsRepository backupsRepo,
        IDialogService dialogs,
        IToastService toasts,
        IErrorReporter errors,
        IBusyService busy,
        ICurrentUserService currentUser,
        IConnectionMonitor connection,
        IUserPreferencesService prefs)
    {
        _backupService = backupService;
        _backupsRepo = backupsRepo;
        _dialogs = dialogs;
        _toasts = toasts;
        _errors = errors;
        _busy = busy;
        _currentUser = currentUser;
        _connection = connection;
        _prefs = prefs;

        // Initialise the backing field directly so OnBackupFolderChanged does
        // not fire (avoids writing prefs.json before the user actually changes
        // the folder).
        _backupFolder = ResolveInitialBackupFolder();

        _currentUser.SignedIn  += OnCurrentUserChanged;
        _currentUser.SignedOut += OnCurrentUserChanged;

        _isInitializing = false;
    }

    public override string TitleAr => "النسخ الاحتياطي";

    public override string SubtitleAr
        => $"النسخ المسجَّلة: {Backups.Count} • مجلد الحفظ: {BackupFolder ?? "—"}";

    public ObservableCollection<BackupLogRow> Backups { get; } = new();

    [ObservableProperty]
    private string? _backupFolder;

    [ObservableProperty]
    private bool _hasBackups;

    [ObservableProperty]
    private bool _isEmpty;

    public bool CanManageBackup => _currentUser.HasPermission(Permission.ManageBackup);

    partial void OnBackupFolderChanged(string? value)
    {
        if (_isInitializing) return;
        _prefs.Current.BackupFolder = string.IsNullOrWhiteSpace(value) ? null : value;
        _prefs.Save();
        OnPropertyChanged(nameof(SubtitleAr));
    }

    public override Task ActivateAsync(CancellationToken ct = default) => ReloadAsync(ct);

    // ====================================================================
    // Commands
    // ====================================================================

    [RelayCommand]
    private async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight) return;
        _reloadInFlight = true;

        try
        {
            if (!CanManageBackup)
            {
                Backups.Clear();
                UpdateEmptyState();
                return;
            }

            IsLoading = true;
            var rows = await _backupsRepo.ListAsync(100, ct).ConfigureAwait(true);

            Backups.Clear();
            foreach (var r in rows) Backups.Add(r);
            UpdateEmptyState();
            OnPropertyChanged(nameof(SubtitleAr));
            _connection.ReportSuccess();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (Exception ex)
        {
            _connection.ReportFailure(ex.Message);
            _errors.Report("تعذّر تحميل سجل النسخ الاحتياطي", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
        }
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        if (!CanManageBackup)
        {
            _toasts.Warning("صلاحية مرفوضة", "ليس لديك صلاحية إدارة النسخ الاحتياطي.");
            return;
        }

        var picked = PickFolder(BackupFolder);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            BackupFolder = picked;
            _toasts.Info("تم تحديث المجلد", picked);
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!CanManageBackup)
        {
            _toasts.Warning("صلاحية مرفوضة", "ليس لديك صلاحية إدارة النسخ الاحتياطي.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupFolder))
        {
            _toasts.Warning("مجلد غير محدد", "الرجاء اختيار مجلد لحفظ النسخة أولاً.");
            return;
        }

        // Ask for optional notes via a small dialog.
        var notes = BackupNotesDialog.Show(Application.Current?.MainWindow);
        if (notes is null) return; // user cancelled

        try
        {
            await _busy.RunAsync(async () =>
            {
                // Progress messages flow into the StatusMessage strip; the busy
                // overlay's headline stays static ("جاري إنشاء النسخة…") so the
                // user always sees a stable label.
                var progress = new Progress<string>(msg => StatusMessage = msg);

                var result = await _backupService
                    .CreateBackupAsync(BackupFolder!, notes, progress)
                    .ConfigureAwait(true);

                _toasts.Success(
                    "تم إنشاء النسخة الاحتياطية",
                    $"تم حفظ الملف:\n{result.FilePath}");
            }, "جاري إنشاء النسخة الاحتياطية…").ConfigureAwait(true);

            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر إنشاء النسخة الاحتياطية", ex.Message, ex);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (!CanManageBackup)
        {
            _toasts.Warning("صلاحية مرفوضة", "ليس لديك صلاحية إدارة النسخ الاحتياطي.");
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "اختر ملف نسخة احتياطية للاستعادة",
            Filter = "ملفات النسخ الاحتياطي (*.bak)|*.bak|كل الملفات (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true
        };
        if (!string.IsNullOrWhiteSpace(BackupFolder) && Directory.Exists(BackupFolder))
            dlg.InitialDirectory = BackupFolder!;

        var owner = Application.Current?.MainWindow;
        var picked = owner is not null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
        if (picked != true) return;

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "تأكيد استعادة النسخة الاحتياطية",
            "سيتم استبدال قاعدة البيانات الحالية بالكامل بمحتويات الملف المختار، وسيتم إغلاق التطبيق فور انتهاء العملية. هل تريد المتابعة؟",
            okText: "نعم، استعادة وإغلاق").ConfigureAwait(true);
        if (!ok) return;

        RestoreResult? result = null;
        try
        {
            await _busy.RunAsync(async () =>
            {
                var progress = new Progress<string>(msg => StatusMessage = msg);

                result = await _backupService
                    .RestoreBackupAsync(dlg.FileName, progress)
                    .ConfigureAwait(true);
            }, "جاري استعادة قاعدة البيانات…").ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
            return;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر استعادة النسخة الاحتياطية", ex.Message, ex);
            return;
        }

        if (result is null) return;

        if (!result.Success)
        {
            await _dialogs.ShowErrorAsync(
                "فشل الاستعادة",
                result.ErrorMessage ?? "حدث خطأ غير معروف أثناء استعادة قاعدة البيانات.")
                .ConfigureAwait(true);
            return;
        }

        // Design decision: after a successful restore, the running EF Core
        // context state, identity columns and even the current user record
        // could all have shifted. The safest UX is to inform the user and
        // shut down the app so they re-launch cleanly.
        NasaqDialog.Show(
            Application.Current?.MainWindow,
            "تمت الاستعادة بنجاح",
            "تمت استعادة قاعدة البيانات بنجاح. سيتم إغلاق التطبيق الآن — الرجاء تشغيله من جديد.",
            NasaqDialogKind.Success);

        Application.Current?.Shutdown(0);
    }

    [RelayCommand]
    private async Task DeleteLogAsync(BackupLogRow? row)
    {
        if (row is null) return;
        if (!CanManageBackup)
        {
            _toasts.Warning("صلاحية مرفوضة", "ليس لديك صلاحية إدارة النسخ الاحتياطي.");
            return;
        }

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف سجل النسخة",
            "سيتم حذف هذا السجل من قائمة النسخ الاحتياطية. لن يتم حذف ملف .bak على القرص. هل تريد المتابعة؟")
            .ConfigureAwait(true);
        if (!ok) return;

        try
        {
            await _backupsRepo.DeleteLogAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم الحذف", "تم حذف السجل من القائمة.");
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر حذف سجل النسخة", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void OpenFolder(BackupLogRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.FilePath)) return;
        try
        {
            // /select highlights the file in Explorer. If the file no longer
            // exists, fall back to opening the parent folder.
            if (File.Exists(row.FilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{row.FilePath}\"")
                {
                    UseShellExecute = true
                });
            }
            else
            {
                var dir = Path.GetDirectoryName(row.FilePath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"")
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    _toasts.Warning("الملف غير موجود", row.FilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح المجلد", ex.Message, ex);
        }
    }

    // ====================================================================
    // Helpers
    // ====================================================================

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanManageBackup));
        ReloadCommand.NotifyCanExecuteChanged();
    }

    private void UpdateEmptyState()
    {
        HasBackups = Backups.Count > 0;
        IsEmpty = !HasBackups && !IsLoading;
    }

    private string ResolveInitialBackupFolder()
    {
        var saved = _prefs.Current.BackupFolder;
        if (!string.IsNullOrWhiteSpace(saved)) return saved!;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Nasaq",
            "Backups");
    }

    private static string? PickFolder(string? initial)
    {
        // .NET 8 ships Microsoft.Win32.OpenFolderDialog inside PresentationFramework
        // — preferred over System.Windows.Forms.FolderBrowserDialog. If for some
        // reason the runtime lacks the type, fall back to the WinForms picker.
        try
        {
            var dlg = new OpenFolderDialog
            {
                Title = "اختر مجلد لحفظ النسخ الاحتياطية",
                Multiselect = false
            };
            if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial))
                dlg.InitialDirectory = initial!;
            return dlg.ShowDialog() == true ? dlg.FolderName : null;
        }
        catch (TypeLoadException)
        {
            return PickFolderWinFormsFallback(initial);
        }
        catch (MissingMethodException)
        {
            return PickFolderWinFormsFallback(initial);
        }
    }

    private static string? PickFolderWinFormsFallback(string? initial)
    {
        // We avoid a hard reference to System.Windows.Forms by going through
        // reflection so the project's csproj doesn't need WinForms enabled.
        // In practice .NET 8 WPF always exposes OpenFolderDialog, so this
        // branch is essentially dead code — kept as a safety net per spec.
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Windows.Forms");
        if (asm is null) return null;

        var t = asm.GetType("System.Windows.Forms.FolderBrowserDialog");
        if (t is null) return null;

        dynamic dlg = Activator.CreateInstance(t)!;
        try
        {
            dlg.Description = "اختر مجلد لحفظ النسخ الاحتياطية";
            if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial))
                dlg.SelectedPath = initial!;
            var dr = dlg.ShowDialog();
            // DialogResult.OK == 1
            if ((int)dr == 1) return (string?)dlg.SelectedPath;
        }
        catch { /* best-effort */ }
        return null;
    }
}
