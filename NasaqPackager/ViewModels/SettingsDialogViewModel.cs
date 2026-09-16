using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NasaqPackager.Services;

namespace NasaqPackager.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly IPackagerSettings _settings;

    [ObservableProperty] private string _projectPath = "";
    [ObservableProperty] private string _releasesPath = "";
    [ObservableProperty] private string _iconPath = "";
    [ObservableProperty] private string _channel = "win";
    [ObservableProperty] private string _runtimeIdentifier = "win-x64";
    [ObservableProperty] private bool _selfContained = true;
    [ObservableProperty] private bool _runObfuscar;
    [ObservableProperty] private string _packId = "Nasaq";
    [ObservableProperty] private string _packTitle = "";

    public ObservableCollection<string> Rids { get; } = new() { "win-x64", "win-x86", "win-arm64" };

    public bool? DialogResult { get; private set; }

    public event System.Action? RequestClose;

    public SettingsDialogViewModel(IPackagerSettings settings)
    {
        _settings = settings;
        ProjectPath = settings.ProjectPath;
        ReleasesPath = settings.ReleasesPath;
        IconPath = settings.IconPath;
        Channel = settings.Channel;
        RuntimeIdentifier = settings.RuntimeIdentifier;
        SelfContained = settings.SelfContained;
        RunObfuscar = settings.RunObfuscar;
        PackId = settings.PackId;
        PackTitle = settings.PackTitle;
    }

    [RelayCommand]
    private void BrowseProject()
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر ملف Nasag.csproj",
            Filter = "C# Project|*.csproj|All files|*.*",
        };
        if (dlg.ShowDialog() == true)
            ProjectPath = dlg.FileName;
    }

    [RelayCommand]
    private void BrowseReleases()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "اختر مجلد الإصدارات",
        };
        if (dlg.ShowDialog() == true)
            ReleasesPath = dlg.FolderName;
    }

    [RelayCommand]
    private void BrowseIcon()
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر ملف الأيقونة",
            Filter = "Icon / Image|*.ico;*.png;*.jpg;*.jpeg|All files|*.*",
        };
        if (dlg.ShowDialog() == true)
            IconPath = dlg.FileName;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.ProjectPath = ProjectPath?.Trim() ?? "";
        _settings.ReleasesPath = ReleasesPath?.Trim() ?? "";
        _settings.IconPath = IconPath?.Trim() ?? "";
        _settings.Channel = string.IsNullOrWhiteSpace(Channel) ? "win" : Channel.Trim();
        _settings.RuntimeIdentifier = string.IsNullOrWhiteSpace(RuntimeIdentifier) ? "win-x64" : RuntimeIdentifier.Trim();
        _settings.SelfContained = SelfContained;
        _settings.RunObfuscar = RunObfuscar;
        _settings.PackId = string.IsNullOrWhiteSpace(PackId) ? "Nasaq" : PackId.Trim();
        _settings.PackTitle = string.IsNullOrWhiteSpace(PackTitle) ? "نَسَق لإدارة المدارس" : PackTitle.Trim();
        _settings.Save();
        DialogResult = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke();
    }
}
