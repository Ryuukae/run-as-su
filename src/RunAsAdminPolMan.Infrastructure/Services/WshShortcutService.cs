using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Synthesizes .lnk shortcut files using dynamically loaded WScript.Shell COM interop.
/// </summary>
public class WshShortcutService : IShortcutService
{
    private readonly ILogger<WshShortcutService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WshShortcutService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public WshShortcutService(ILogger<WshShortcutService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> CreateShortcutAsync(string shortcutPath, string targetPath, string arguments, string iconLocation, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                string directory = Path.GetDirectoryName(shortcutPath) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _logger.LogDebug("Instantiating WScript.Shell COM object via Type.GetTypeFromProgID.");
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    _logger.LogError("WScript.Shell COM type not found on this system.");
                    return Result.Fail(new Error("COM_NOT_FOUND", "Windows Script Host is missing or disabled on this system."));
                }

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null)
                {
                    return Result.Fail(new Error("COM_INSTANTIATION_ERROR", "Failed to instantiate WScript.Shell."));
                }

                _logger.LogInformation("Creating shortcut at {ShortcutPath}", shortcutPath);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;

                if (!string.IsNullOrWhiteSpace(iconLocation))
                {
                    shortcut.IconLocation = iconLocation;
                }

                shortcut.WindowStyle = 1; // Normal window
                shortcut.Save();

                // Explicit COM release is not strictly necessary with `dynamic` in simple scenarios, 
                // but good practice if looping. The GC will clean it up.

                _logger.LogInformation("Shortcut synthesized successfully.");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shortcut at {ShortcutPath}.", shortcutPath);
                return Result.Fail(new Error("SHORTCUT_CREATION_ERROR", "An unexpected error occurred while creating the .lnk file."));
            }
        }, cancellationToken);
    }
}