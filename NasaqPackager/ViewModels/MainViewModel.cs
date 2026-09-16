using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqPackager.Services;
using NasaqPackager.Views;

namespace NasaqPackager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPackagerSettings _settings;
    private readonly IProjectVersionService _versionService;
    private readonly IPipelineRunner _runner;

    [ObservableProperty] private string _currentVersion = "—";
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _projectPath = "";
    [ObservableProperty] private string _releasesPath = "";
    [ObservableProperty] private string _iconPath = "";
    [ObservableProperty] private string _channel = "win";

    public MainViewModel(
        IPackagerSettings settings,
        IProjectVersionService versionService,
        IPipelineRunner runner)
    {
        _settings = settings;
        _versionService = versionService;
        _runner = runner;

        LoadFromSettings();
        RefreshVersion();
        AppendLog("مرحباً بك في أداة تحزيم نَسَق.");
        AppendLog($"مشروع: {ProjectPath}");
        AppendLog($"الإصدارات: {ReleasesPath}");
    }

    private void LoadFromSettings()
    {
        ProjectPath = _settings.ProjectPath;
        ReleasesPath = _settings.ReleasesPath;
        IconPath = _settings.IconPath;
        Channel = _settings.Channel;
    }

    private void RefreshVersion()
    {
        try
        {
            var v = _versionService.GetCurrentVersion(ProjectPath);
            CurrentVersion = string.IsNullOrWhiteSpace(v) ? "—" : v!;
        }
        catch (Exception ex)
        {
            CurrentVersion = "—";
            AppendLog($"[خطأ] تعذّر قراءة رقم الإصدار: {ex.Message}");
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        BumpVersionCommand.NotifyCanExecuteChanged();
        PackageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task BumpVersionAsync()
    {
        try
        {
            IsBusy = true;
            await Task.Run(() =>
            {
                var newV = _versionService.BumpPatch(ProjectPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentVersion = newV;
                    AppendLog($"[تم] تم زيادة رقم النسخة إلى {newV}.");
                });
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[خطأ] فشل زيادة رقم النسخة: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PackageAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath) || !File.Exists(ProjectPath))
        {
            AppendLog("[خطأ] مسار المشروع غير صالح. افتح الإعدادات وحدّد ملف Nasag.csproj.");
            return;
        }

        try
        {
            IsBusy = true;
            RefreshVersion();
            var version = CurrentVersion == "—" ? "1.14.0" : CurrentVersion;

            var cfg = new PipelineConfig(
                ProjectPath: ProjectPath,
                PackId: _settings.PackId,
                PackTitle: _settings.PackTitle,
                Version: version,
                ReleasesPath: ReleasesPath,
                Channel: Channel,
                IconPath: IconPath,
                Rid: _settings.RuntimeIdentifier,
                SelfContained: _settings.SelfContained,
                RunObfuscar: _settings.RunObfuscar);

            using var cts = new CancellationTokenSource();
            await foreach (var line in _runner.RunPipelineAsync(cfg, cts.Token))
            {
                AppendLog(line);
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[خطأ] فشل التحزيم: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenReleases()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ReleasesPath))
            {
                AppendLog("[خطأ] مسار الإصدارات غير محدد.");
                return;
            }

            if (!Directory.Exists(ReleasesPath))
                Directory.CreateDirectory(ReleasesPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{ReleasesPath}\"",
                UseShellExecute = true,
            });
            AppendLog($"[تم] فتح مجلد الإصدارات: {ReleasesPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"[خطأ] تعذّر فتح المجلد: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsDialogViewModel(_settings);
        var dialog = new SettingsDialog { DataContext = vm };
        if (Application.Current?.MainWindow is { } main && !ReferenceEquals(main, dialog))
            dialog.Owner = main;
        var ok = dialog.ShowDialog();
        if (ok == true)
        {
            LoadFromSettings();
            RefreshVersion();
            AppendLog("[تم] تم حفظ الإعدادات.");
        }
    }

    [RelayCommand]
    private void CopyLog()
    {
        try
        {
            Clipboard.SetText(LogText ?? "");
            AppendLog("[تم] تم نسخ السجل إلى الحافظة.");
        }
        catch (Exception ex)
        {
            AppendLog($"[خطأ] تعذّر النسخ: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogText = "";
    }

    private void AppendLog(string line)
    {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(() => AppendLog(line));
            return;
        }

        var stamp = DateTime.Now.ToString("HH:mm:ss");
        LogText += $"[{stamp}] {line}\n";
    }
}
