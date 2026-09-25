using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.ViewModels;

/// <summary>
/// The main view model for the application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IRegistryService _registryService;
    private readonly IAppMetadataService _metadataService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUacBypassOrchestrator _uacOrchestrator;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<AppPolicy> _policies = new();

    [ObservableProperty]
    private AppPolicy? _selectedPolicy;

    [ObservableProperty]
    private PolicyScope _currentScope = PolicyScope.CurrentUser;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    private readonly SettingsViewModel _settingsViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        ILogger<MainViewModel> logger,
        IRegistryService registryService,
        IAppMetadataService metadataService,
        IFileDialogService fileDialogService,
        IUacBypassOrchestrator uacOrchestrator,
        ISettingsService settingsService,
        SettingsViewModel settingsViewModel)
    {
        _logger = logger;
        _registryService = registryService;
        _metadataService = metadataService;
        _fileDialogService = fileDialogService;
        _uacOrchestrator = uacOrchestrator;
        _settingsService = settingsService;
        _settingsViewModel = settingsViewModel;

        _currentViewModel = this;

        _logger.LogInformation("MainViewModel initialized.");
    }

    /// <summary>
    /// Toggles the visible inline view between the main policy grid and the settings menu.
    /// </summary>
    [RelayCommand]
    public async Task ToggleSettingsViewAsync()
    {
        if (CurrentViewModel == this)
        {
            await _settingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
            CurrentViewModel = _settingsViewModel;
            StatusMessage = "Settings configuration opened.";
            _logger.LogInformation("Navigated to Settings View.");
        }
        else
        {
            CurrentViewModel = this;
            StatusMessage = "Ready";
            _logger.LogInformation("Navigated to Main View.");
        }
    }

    /// <summary>
    /// Loads policies from the registry for the current scope.
    /// </summary>
    [RelayCommand]
    public async Task LoadPoliciesAsync()
    {
        _logger.LogInformation("Initiating LoadPoliciesAsync for scope: {Scope}", CurrentScope);
        StatusMessage = "Loading policies...";
        var result = await _registryService.GetPoliciesAsync(CurrentScope);

        if (result.IsSuccess)
        {
            var policyList = result.Value.ToList();
            _logger.LogInformation("Retrieved {Count} raw policies from registry.", policyList.Count);

            // Throttle to prevent ThreadPool starvation and OOM crashes on massive registries
            using var semaphore = new System.Threading.SemaphoreSlim(10);

            var enrichTasks = policyList.Select(async p =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug("Extracting metadata for policy: {FilePath}", p.FilePath);
                    var metaResult = await _metadataService.ExtractMetadataAsync(p);
                    if (metaResult.IsSuccess)
                    {
                        _logger.LogDebug("Successfully enriched metadata for {FilePath}.", p.FilePath);
                        return metaResult.Value;
                    }
                    _logger.LogWarning("Failed to enrich metadata for {FilePath}: {Error}", p.FilePath, metaResult.Error?.Code);
                    return p;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var enrichedList = await Task.WhenAll(enrichTasks);

            Policies = new ObservableCollection<AppPolicy>(enrichedList);
            StatusMessage = $"Loaded {Policies.Count} policies.";
        }
        else
        {
            StatusMessage = $"Error: {result.Error?.Message}";
        }
    }

    /// <summary>
    /// Removes the selected policy from the registry.
    /// </summary>
    [RelayCommand]
    public async Task RemovePolicyAsync()
    {
        if (SelectedPolicy is null) return;

        _logger.LogInformation("Attempting to remove policy for {FilePath} in scope {Scope}", SelectedPolicy.FilePath, CurrentScope);
        StatusMessage = $"Removing policy for {SelectedPolicy.FilePath}...";
        var result = await _registryService.SetPolicyAsync(SelectedPolicy.FilePath, enable: false, CurrentScope);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully removed policy for {FilePath}.", SelectedPolicy.FilePath);
            Policies.Remove(SelectedPolicy);
            StatusMessage = "Policy removed successfully.";
        }
        else
        {
            _logger.LogWarning("Failed to remove policy for {FilePath}: {Error}", SelectedPolicy.FilePath, result.Error?.Code);
            StatusMessage = $"Error: {result.Error?.Message}";
        }
    }

    /// <summary>
    /// Adds multiple new policies to the registry and refreshes the view once.
    /// </summary>
    /// <param name="filePaths">The collection of file paths to add.</param>
    [RelayCommand]
    public async Task AddPoliciesAsync(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        _logger.LogInformation("Attempting to add policies for {Count} files in scope {Scope}", filePaths.Count(), CurrentScope);
        StatusMessage = "Enabling policies...";
        bool anySuccess = false;
        string? lastError = null;

        foreach (var path in filePaths)
        {
            _logger.LogDebug("Setting policy for: {FilePath}", path);
            var result = await _registryService.SetPolicyAsync(path, enable: true, CurrentScope);
            if (result.IsSuccess)
            {
                anySuccess = true;
            }
            else
            {
                _logger.LogWarning("Failed to set policy for {FilePath}: {Error}", path, result.Error?.Code);
                lastError = result.Error?.Message;
            }
        }

        if (anySuccess)
        {
            await LoadPoliciesCommand.ExecuteAsync(null);
        }
        else if (lastError != null)
        {
            StatusMessage = $"Error: {lastError}";
        }
    }

    /// <summary>
    /// Toggles the registry scope between HKCU and HKLM.
    /// </summary>
    [RelayCommand]
    public async Task ToggleScopeAsync()
    {
        CurrentScope = CurrentScope == PolicyScope.CurrentUser ? PolicyScope.LocalMachine : PolicyScope.CurrentUser;
        _logger.LogInformation("Toggled policy scope to: {Scope}", CurrentScope);
        await LoadPoliciesCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Opens the native file dialog to manually select and add policies.
    /// </summary>
    [RelayCommand]
    public async Task OpenFileDialogAndAddPoliciesAsync()
    {
        _logger.LogDebug("Opening native file dialog...");
        var files = _fileDialogService.ShowOpenExeDialog();
        if (files != null && files.Length > 0)
        {
            _logger.LogInformation("File dialog returned {Count} files.", files.Length);
            await AddPoliciesCommand.ExecuteAsync(files);
        }
        else
        {
            _logger.LogDebug("File dialog canceled or empty.");
        }
    }
    /// <summary>
    /// Generates a UAC bypass shortcut for the selected policy.
    /// </summary>
    [RelayCommand]
    public async Task CreateUacShortcutAsync()
    {
        if (SelectedPolicy == null)
        {
            StatusMessage = "Please select a policy to create a UAC bypass shortcut.";
            return;
        }

        StatusMessage = "Creating UAC Bypass shortcut...";

        var settingsResult = await _settingsService.GetSettingsAsync();
        string defaultPath = settingsResult.IsSuccess ? settingsResult.Value.DefaultShortcutLocation : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        string? shortcutPath = _fileDialogService.ShowSaveFileDialog("Save UAC Bypass Shortcut", defaultPath, SelectedPolicy.ProductName + ".lnk", "Shortcut Files (*.lnk)|*.lnk");

        if (string.IsNullOrWhiteSpace(shortcutPath))
        {
            StatusMessage = "Shortcut creation cancelled.";
            return;
        }

        var result = await _uacOrchestrator.GenerateUacBypassShortcutAsync(SelectedPolicy, System.IO.Path.GetDirectoryName(shortcutPath) ?? defaultPath);

        if (result.IsSuccess)
        {
            StatusMessage = "UAC Bypass Shortcut successfully created at: " + shortcutPath;
            _logger.LogInformation("UAC Bypass Shortcut successfully generated for {Product} at {Path}", SelectedPolicy.ProductName, shortcutPath);
        }
        else
        {
            StatusMessage = "Failed to create shortcut: " + result.Error?.Message;
            _logger.LogError("UAC Bypass failed: {Error}", result.Error?.Message);
        }
    }
}