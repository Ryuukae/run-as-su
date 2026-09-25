using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.Infrastructure.Services;

using Serilog;

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
        // Setup Serilog
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RunAsAdminPolMan", "Logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Initializing Application...");

        // Wire up global crash handlers
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Dispatcher Unhandled Exception");
            Log.CloseAndFlush();
            e.Handled = false;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Error(e.Exception, "Unobserved Task Exception");
            // Don't crash for unobserved task exceptions, just log them
            e.SetObserved();
        };

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register Infrastructure Services
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<ISecurityService, SecurityService>();
                services.AddSingleton<IAppMetadataService, AppMetadataService>();
                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<ISettingsService, JsonSettingsService>();
                services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();
                services.AddSingleton<IShortcutService, WshShortcutService>();
                services.AddSingleton<IUacBypassOrchestrator, UacBypassOrchestrator>();
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
        Log.Information("Application Startup Initiated.");

        const string mutexName = "Global\\RunAsAdminPolMan_SingleInstance_Mutex";
        bool createdNew;
        try
        {
            _instanceMutex = new System.Threading.Mutex(true, mutexName, out createdNew);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Mutex UnauthorizedAccessException trapped. Another user session is holding the global mutex.");
            createdNew = false;
        }

        if (!createdNew)
        {
            Log.Warning("Duplicate instance detected. Terminating.");
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
        Log.Information("Application Shutdown Initiated.");

        await Host.StopAsync();
        Host.Dispose();

        if (_instanceMutex != null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to release Mutex.");
            }
            _instanceMutex.Dispose();
        }

        Log.Information("Application Shutdown Complete. Flushing logs.");
        Log.CloseAndFlush();

        base.OnExit(e);
    }
}