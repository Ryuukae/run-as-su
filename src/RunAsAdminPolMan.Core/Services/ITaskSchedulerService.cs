using System.Threading;
using System.Threading.Tasks;

using RunAsAdminPolMan.Core.Models;

namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Service for creating elevated Windows Scheduled Tasks for UAC bypassing.
/// </summary>
public interface ITaskSchedulerService
{
    /// <summary>
    /// Creates or overwrites a Scheduled Task configured to run with highest privileges.
    /// </summary>
    /// <param name="policy">The policy containing the application to launch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result containing the generated Task Name if successful.</returns>
    Task<Result<string>> CreateElevatedTaskAsync(AppPolicy policy, CancellationToken cancellationToken = default);
}