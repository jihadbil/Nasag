using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Services;
using Nasag.Services.Licensing;

namespace Nasag.ViewModels.Licensing;

/// <summary>
/// نافذة التحديثات — فحص/تنزيل/تطبيق عبر <see cref="IUpdateService"/>.
/// </summary>
public sealed partial class UpdateViewModel : ObservableObject
{
    private readonly IUpdateService _updates;
    private readonly IErrorReporter _errors;
    private CancellationTokenSource? _cts;

    public UpdateViewModel(IUpdateService updates, IErrorReporter errors)
    {
        _updates = updates;
        _errors = errors;
        CurrentVersion = _updates.CurrentVersion;
    }

    /// <summary>طلب الإغلاق من النافذة.</summary>
    public Action? RequestClose { get; set; }

    [ObservableProperty] private string _currentVersion = "1.14.0";
    [ObservableProperty] private string? _newVersion;
    [ObservableProperty] private string? _releaseNotes;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isChecking;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _hasUpdate;
    [ObservableProperty] private bool _checkedNoUpdate;
    [ObservableProperty] private int _progress;

    [RelayCommand]
    public async Task CheckAsync()
    {
        ErrorMessage = null;
        HasUpdate = false;
        CheckedNoUpdate = false;
        IsChecking = true;
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var result = await _updates.CheckAsync(_cts.Token).ConfigureAwait(true);
            if (result.HasUpdate)
            {
                NewVersion = result.NewVersion;
                ReleaseNotes = result.Notes;
                HasUpdate = true;
            }
            else
            {
                CheckedNoUpdate = true;
            }
        }
        catch (OperationCanceledException)
        {
            // تجاهل
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذّر التحقّق من التحديثات: {ex.Message}";
            _errors.Report("تعذّر التحقّق من التحديثات", ex.Message, ex);
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        ErrorMessage = null;
        IsDownloading = true;
        Progress = 0;
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var progress = new Progress<int>(p => Progress = p);
            await _updates.DownloadAsync(progress, _cts.Token).ConfigureAwait(true);

            IsDownloaded = true;
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            // تجاهل
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذّر تنزيل التحديث: {ex.Message}";
            _errors.Report("تعذّر تنزيل التحديث", ex.Message, ex);
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void ApplyAndRestart()
    {
        try
        {
            _updates.ApplyAndRestart();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذّر تطبيق التحديث: {ex.Message}";
            _errors.Report("تعذّر تطبيق التحديث", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void Close()
    {
        try { _cts?.Cancel(); } catch { /* تجاهل */ }
        RequestClose?.Invoke();
    }
}
