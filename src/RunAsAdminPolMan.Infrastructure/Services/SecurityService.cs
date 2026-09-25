using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the ISecurityService using Cryptography.Pkcs APIs.
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// </summary>
    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> VerifySignatureAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return Result<bool>.Fail(new Error("FILE_NOT_FOUND", "The specified executable file does not exist."));
            }

            try
            {
                // Authenticode signatures are embedded PKCS #7 signatures.
                // We use X509Certificate.CreateFromSignedFile to check if a signature exists and is structurally valid.
                using var cert = X509Certificate.CreateFromSignedFile(filePath);
                
                // For a deeper validation (trust chain), we can instantiate an X509Certificate2 and verify.
                using var cert2 = new X509Certificate2(cert);
                bool isValid = cert2.Verify();

                return Result<bool>.Success(isValid);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                // This exception is thrown if the file is unsigned or the signature is corrupted.
                _logger.LogInformation(ex, "File {FilePath} is not signed or has an invalid signature.", filePath);
                return Result<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify signature for {FilePath}.", filePath);
                return Result<bool>.Fail(new Error("SIG_VERIFY_ERROR", "An unexpected error occurred while verifying the signature."));
            }
        }, cancellationToken);
    }
}
