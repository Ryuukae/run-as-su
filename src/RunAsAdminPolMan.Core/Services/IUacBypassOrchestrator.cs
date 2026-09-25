using System.Threading;
using System.Threading.Tasks;

using RunAsAdminPolMan.Core.Models;

namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Orchestrates the creation of UAC Bypass shortcuts by enforcing security mitigations
/// and coordinating the Task Scheduler and Shortcut engines.
/// </summary>
public interface IUacBypassOrchestrator
{
    /// <summary>
    /// Executes the full UAC Bypass shortcut creation workflow.
    /// </summary>
    /// <param name="policy">The application policy.</param>
    /// <param name="shortcutSaveDirectory">The directory to save the .lnk file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> GenerateUacBypassShortcutAsync(AppPolicy policy, string shortcutSaveDirectory, CancellationToken cancellationToken = default);
}