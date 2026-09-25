namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Defines operations for generating and restoring auto-backups.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a `.reg` file backup of the current state of the specified scope asynchronously.
    /// </summary>
    /// <param name="scope">The registry hive scope to backup.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the file path of the generated backup.</returns>
    Task<Result<string>> CreateBackupAsync(PolicyScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the registry state from a previously generated `.reg` backup asynchronously.
    /// </summary>
    /// <param name="backupFilePath">The absolute path to the `.reg` backup file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
    Task<Result> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}