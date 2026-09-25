using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.Infrastructure.Services;

namespace RunAsAdminPolMan;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application host for Dependency Injection.
    /// </summary>
    public IHost Host { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register Infrastructure Services
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<ISecurityService, SecurityService>();
                services.AddSingleton<IAppMetadataService, AppMetadataService>();
                services.AddSingleton<IBackupService, BackupService>();

                // Register ViewModels
                services.AddSingleton<RunAsAdminPolMan.ViewModels.MainViewModel>();

                // Register Views
                services.AddTransient<MainWindow>();
            })
            .Build();
    }

    /// <summary>
    /// Handles the application startup event.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        await Host.StartAsync();

        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Handles the application exit event.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected override async void OnExit(ExitEventArgs e)
    {
        await Host.StopAsync();
        Host.Dispose();

        base.OnExit(e);
    }
}