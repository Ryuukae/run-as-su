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
                services.AddSingleton<IFileDialogService, RunAsAdminPolMan.Services.WpfFileDialogService>();

                // Register ViewModels
                services.AddSingleton<RunAsAdminPolMan.ViewModels.MainViewModel>();

                // Register Views
                services.AddTransient<MainWindow>();
            })
            .Build();
    }

    private System.Threading.Mutex? _instanceMutex;

    /// <summary>
    /// Handles the application startup event.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "Global\\RunAsAdminPolMan_SingleInstance_Mutex";
        bool createdNew;
        try
        {
            _instanceMutex = new System.Threading.Mutex(true, mutexName, out createdNew);
        }
        catch (UnauthorizedAccessException)
        {
            // If another user (like a second RDP admin) holds the mutex, we can't open it.
            // This mathematically proves another instance is already running on the machine.
            createdNew = false;
        }
        
        if (!createdNew)
        {
            MessageBox.Show("Another instance of RunAsAdmin Policy Manager is already running on this machine.", "Instance Already Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            Current.Shutdown();
            return;
        }

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

        if (_instanceMutex != null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch (Exception)
            {
                // Ignore if we didn't own the mutex (e.g. exception during creation)
            }
            _instanceMutex.Dispose();
        }

        base.OnExit(e);
    }
}