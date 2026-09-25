using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the ISecurityService.
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
                using var cert = X509Certificate.CreateFromSignedFile(filePath);
                using var cert2 = new X509Certificate2(cert);
                bool isValid = cert2.Verify();

                return Result<bool>.Success(isValid);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
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

    /// <inheritdoc />
    public Result VerifyPathSecurity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result.Fail(new Error("INVALID_PATH", "Path is invalid."));

        try
        {
            string normalizedPath = Path.GetFullPath(path);
            if (!File.Exists(normalizedPath))
            {
                return Result.Fail(new Error("FILE_NOT_FOUND", "The specified executable file does not exist."));
            }

            // 1. Verify File ACLs
            var fileInfo = new FileInfo(normalizedPath);
            var fileSecurity = fileInfo.GetAccessControl();
            var fileResult = CheckAclSecurity(fileSecurity, normalizedPath, "file");
            if (!fileResult.IsSuccess) return fileResult;

            // 2. Verify Parent Directory ACLs
            string? directoryPath = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                var directorySecurity = directoryInfo.GetAccessControl();
                var dirResult = CheckAclSecurity(directorySecurity, directoryPath, "directory");
                if (!dirResult.IsSuccess) return dirResult;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inspect ACLs for path {Path}.", path);
            return Result.Fail(new Error("ACL_INSPECTION_FAILED", "Could not verify the security of the target path."));
        }
    }

    private Result CheckAclSecurity(CommonObjectSecurity security, string targetPath, string type)
    {
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var builtinUsersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        var interactiveSid = new SecurityIdentifier(WellKnownSidType.InteractiveSid, null);
        var authenticatedUsersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);

        foreach (FileSystemAccessRule rule in rules)
        {
            if ((rule.IdentityReference.Value == everyoneSid.Value ||
                 rule.IdentityReference.Value == builtinUsersSid.Value ||
                 rule.IdentityReference.Value == interactiveSid.Value ||
                 rule.IdentityReference.Value == authenticatedUsersSid.Value) &&
                rule.AccessControlType == AccessControlType.Allow &&
                ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write ||
                 (rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData ||
                 (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify ||
                 (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl))
            {
                return Result.Fail(new Error(
                    "LPE_VULNERABILITY_DETECTED",
                    $"The {type} '{targetPath}' grants write/modify access to standard users. " +
                    $"Executing a UAC bypass for this path is blocked to prevent Local Privilege Escalation."));
            }
        }
        return Result.Success();
    }
}