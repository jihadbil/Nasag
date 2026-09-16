using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nasag.Services;

public interface IBusyService
{
    bool IsBusy { get; }
    string Message { get; }
    event EventHandler? StateChanged;

    Task RunAsync(Func<Task> work, string message);
    Task<T> RunAsync<T>(Func<Task<T>> work, string message);
}

public sealed partial class BusyService : ObservableObject, IBusyService
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _message = "جاري التحميل…";

    public event EventHandler? StateChanged;

    public async Task RunAsync(Func<Task> work, string message)
    {
        try
        {
            Message = message;
            IsBusy = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
            await work().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> work, string message)
    {
        try
        {
            Message = message;
            IsBusy = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return await work().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
