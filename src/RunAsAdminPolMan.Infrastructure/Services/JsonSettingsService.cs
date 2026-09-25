using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the settings service using a local JSON file.
/// </summary>
public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILogger<JsonSettingsService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public JsonSettingsService(ILogger<JsonSettingsService> logger)
    {
        _logger = logger;

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "RunAsAdminPolMan");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    /// <inheritdoc/>
    public async Task<Result<AppSettings>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found at {Path}. Returning default settings.", _settingsFilePath);
                return Result<AppSettings>.Success(new AppSettings());
            }

            _logger.LogDebug("Reading settings from {Path}.", _settingsFilePath);
            string json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);

            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings != null
                ? Result<AppSettings>.Success(settings)
                : Result<AppSettings>.Success(new AppSettings());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}.", _settingsFilePath);
            return Result<AppSettings>.Fail(new Error("SETTINGS_LOAD_ERROR", "An error occurred while loading application settings."));
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Result> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Saving settings to {Path}.", _settingsFilePath);

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}.", _settingsFilePath);
            return Result.Fail(new Error("SETTINGS_SAVE_ERROR", "An error occurred while saving application settings."));
        }
        finally
        {
            _fileLock.Release();
        }
    }
}