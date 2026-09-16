using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NasaqPackager.Services;
using NasaqPackager.ViewModels;

namespace NasaqPackager;

public partial class App : Application
{
    public IHost? Host { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IPackagerSettings, PackagerSettings>();
        builder.Services.AddSingleton<IProjectVersionService, ProjectVersionService>();
        builder.Services.AddSingleton<IPipelineRunner, PipelineRunner>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        Host = builder.Build();
        Host.Start();

        var window = Host.Services.GetRequiredService<MainWindow>();
        window.DataContext = Host.Services.GetRequiredService<MainViewModel>();
        window.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host?.Dispose();
        base.OnExit(e);
    }
}
