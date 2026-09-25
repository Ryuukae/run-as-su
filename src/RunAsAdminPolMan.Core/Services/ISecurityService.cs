namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Defines operations for verifying the digital signatures of executables.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Verifies if the specified executable is signed with a valid Authenticode signature asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute path to the executable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing true if validly signed, false otherwise.</returns>
    Task<Result<bool>> VerifySignatureAsync(string filePath, CancellationToken cancellationToken = default);
}