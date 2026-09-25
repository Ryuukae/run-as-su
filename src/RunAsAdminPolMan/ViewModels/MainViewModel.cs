using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.ViewModels;

/// <summary>
/// The main view model for the application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IRegistryService _registryService;
    private readonly IAppMetadataService _metadataService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private ObservableCollection<AppPolicy> _policies = new();

    [ObservableProperty]
    private AppPolicy? _selectedPolicy;

    [ObservableProperty]
    private PolicyScope _currentScope = PolicyScope.CurrentUser;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(IRegistryService registryService, IAppMetadataService metadataService, IFileDialogService fileDialogService)
    {
        _registryService = registryService;
        _metadataService = metadataService;
        _fileDialogService = fileDialogService;
    }

    /// <summary>
    /// Loads policies from the registry for the current scope.
    /// </summary>
    [RelayCommand]
    public async Task LoadPoliciesAsync()
    {
        StatusMessage = "Loading policies...";
        var result = await _registryService.GetPoliciesAsync(CurrentScope);
        
        if (result.IsSuccess)
        {
            var policyList = result.Value.ToList();
            
            // Enrich with metadata concurrently
            var enrichTasks = policyList.Select(async p => 
            {
                var metaResult = await _metadataService.ExtractMetadataAsync(p);
                return metaResult.IsSuccess ? metaResult.Value : p;
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

        StatusMessage = $"Removing policy for {SelectedPolicy.FilePath}...";
        var result = await _registryService.SetPolicyAsync(SelectedPolicy.FilePath, enable: false, CurrentScope);

        if (result.IsSuccess)
        {
            Policies.Remove(SelectedPolicy);
            StatusMessage = "Policy removed successfully.";
        }
        else
        {
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
        StatusMessage = "Enabling policies...";
        bool anySuccess = false;
        string? lastError = null;

        foreach (var path in filePaths)
        {
            var result = await _registryService.SetPolicyAsync(path, enable: true, CurrentScope);
            if (result.IsSuccess)
            {
                anySuccess = true;
            }
            else
            {
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
        await LoadPoliciesCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Opens the native file dialog to manually select and add policies.
    /// </summary>
    [RelayCommand]
    public async Task OpenFileDialogAndAddPoliciesAsync()
    {
        var files = _fileDialogService.ShowOpenExeDialog();
        if (files != null && files.Length > 0)
        {
            await AddPoliciesCommand.ExecuteAsync(files);
        }
    }
}
