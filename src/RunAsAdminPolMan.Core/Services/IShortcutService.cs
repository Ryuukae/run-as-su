using System.Threading;
using System.Threading.Tasks;

namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Service for generating physical Windows shortcut (.lnk) files.
/// </summary>
public interface IShortcutService
{
    /// <summary>
    /// Synthesizes a desktop shortcut pointing to a specific command.
    /// </summary>
    /// <param name="shortcutPath">The full path where the .lnk should be saved.</param>
    /// <param name="targetPath">The target executable to run (e.g., schtasks.exe).</param>
    /// <param name="arguments">The arguments for the target path.</param>
    /// <param name="iconLocation">The native executable path to clone the icon from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> CreateShortcutAsync(string shortcutPath, string targetPath, string arguments, string iconLocation, CancellationToken cancellationToken = default);
}