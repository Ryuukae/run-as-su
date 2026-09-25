using System.Threading;
using System.Threading.Tasks;

using RunAsAdminPolMan.Core.Models;

namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Provides operations for loading and saving application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Retrieves the application settings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the current settings.</returns>
    Task<Result<AppSettings>> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result if saved successfully.</returns>
    Task<Result> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
}