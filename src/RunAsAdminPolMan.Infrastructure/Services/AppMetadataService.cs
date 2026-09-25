using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the IAppMetadataService.
/// </summary>
public class AppMetadataService : IAppMetadataService
{
    private readonly ILogger<AppMetadataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppMetadataService"/> class.
    /// </summary>
    public AppMetadataService(ILogger<AppMetadataService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppPolicy>> ExtractMetadataAsync(AppPolicy policy, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(policy.FilePath) || !File.Exists(policy.FilePath))
            {
                return Result<AppPolicy>.Fail(new Error("FILE_NOT_FOUND", "The specified executable file does not exist."));
            }

            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(policy.FilePath);

                string productName = fileVersionInfo.ProductName ?? Path.GetFileNameWithoutExtension(policy.FilePath);
                string publisher = fileVersionInfo.CompanyName ?? "Unknown Publisher";

                // Extract Icon (Platform specific, Icon.ExtractAssociatedIcon is Windows-only and supported in WinForms/Drawing)
                byte[]? iconBytes = null;
                try
                {
                    using var icon = Icon.ExtractAssociatedIcon(policy.FilePath);
                    if (icon != null)
                    {
                        using var ms = new MemoryStream();
                        icon.Save(ms);
                        iconBytes = ms.ToArray();
                    }
                }
                catch (Exception iconEx)
                {
                    _logger.LogWarning(iconEx, "Failed to extract icon for {FilePath}", policy.FilePath);
                }

                var enrichedPolicy = policy with
                {
                    ProductName = productName,
                    Publisher = publisher,
                    IconBytes = iconBytes
                };

                return Result<AppPolicy>.Success(enrichedPolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract metadata for {FilePath}.", policy.FilePath);
                return Result<AppPolicy>.Fail(new Error("METADATA_EXTRACT_ERROR", "An unexpected error occurred while extracting application metadata."));
            }
        }, cancellationToken);
    }
}