namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Defines operations for extracting metadata (icons, publisher) from executable files.
/// </summary>
public interface IAppMetadataService
{
    /// <summary>
    /// Enriches an AppPolicy with the product name, publisher, and icon from the physical executable asynchronously.
    /// </summary>
    /// <param name="policy">The AppPolicy model to enrich.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the enriched AppPolicy.</returns>
    Task<Result<AppPolicy>> ExtractMetadataAsync(AppPolicy policy, CancellationToken cancellationToken = default);
}