using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.ViewModels;

/// <summary>
/// ViewModel for managing application settings.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _defaultShortcutLocation = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Loads the current settings.
    /// </summary>
    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        _logger.LogInformation("Loading settings in SettingsViewModel.");
        var result = await _settingsService.GetSettingsAsync();

        if (result.IsSuccess && result.Value != null)
        {
            DefaultShortcutLocation = result.Value.DefaultShortcutLocation;
            StatusMessage = "Settings loaded.";
        }
        else
        {
            StatusMessage = $"Failed to load settings: {result.Error?.Message}";
        }
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        _logger.LogInformation("Saving settings from SettingsViewModel.");

        var settings = new AppSettings
        {
            DefaultShortcutLocation = DefaultShortcutLocation
        };

        var result = await _settingsService.SaveSettingsAsync(settings);

        if (result.IsSuccess)
        {
            StatusMessage = "Settings saved successfully.";
        }
        else
        {
            StatusMessage = $"Failed to save settings: {result.Error?.Message}";
        }
    }
}