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
    public MainViewModel(IRegistryService registryService, IAppMetadataService metadataService)
    {
        _registryService = registryService;
        _metadataService = metadataService;
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
    /// Adds a new policy to the registry.
    /// </summary>
    /// <param name="filePath">The file path to add.</param>
    public async Task AddPolicyAsync(string filePath)
    {
        StatusMessage = $"Enabling policy for {filePath}...";
        var result = await _registryService.SetPolicyAsync(filePath, enable: true, CurrentScope);

        if (result.IsSuccess)
        {
            await LoadPoliciesAsync();
        }
        else
        {
            StatusMessage = $"Error: {result.Error?.Message}";
        }
    }
    
    /// <summary>
    /// Toggles the registry scope between HKCU and HKLM.
    /// </summary>
    [RelayCommand]
    public async Task ToggleScopeAsync()
    {
        CurrentScope = CurrentScope == PolicyScope.CurrentUser ? PolicyScope.LocalMachine : PolicyScope.CurrentUser;
        await LoadPoliciesAsync();
    }
}
