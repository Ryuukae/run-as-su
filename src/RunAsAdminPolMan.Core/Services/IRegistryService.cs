namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Defines operations for interacting with the Windows Registry AppCompatFlags.
/// </summary>
public interface IRegistryService
{
    /// <summary>
    /// Retrieves all RunAsAdmin policies for the specified scope asynchronously.
    /// </summary>
    /// <param name="scope">The registry hive scope.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
    Task<Result<IEnumerable<AppPolicy>>> GetPoliciesAsync(PolicyScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a policy to run as Administrator asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute path to the executable.</param>
    /// <param name="enable">True to enable (~ RUNASADMIN), false to disable (-RUNASADMIN).</param>
    /// <param name="scope">The registry hive scope.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
    Task<Result> SetPolicyAsync(string filePath, bool enable, PolicyScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a policy from the registry entirely asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute path to the executable.</param>
    /// <param name="scope">The registry hive scope.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
    Task<Result> RemovePolicyAsync(string filePath, PolicyScope scope, CancellationToken cancellationToken = default);
}