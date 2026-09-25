using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Orchestrates the UAC bypass workflow and enforces Local Privilege Escalation (LPE) security mitigations.
/// </summary>
public class UacBypassOrchestrator : IUacBypassOrchestrator
{
    private readonly ITaskSchedulerService _taskService;
    private readonly IShortcutService _shortcutService;
    private readonly ILogger<UacBypassOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UacBypassOrchestrator"/> class.
    /// </summary>
    public UacBypassOrchestrator(
        ITaskSchedulerService taskService,
        IShortcutService shortcutService,
        ILogger<UacBypassOrchestrator> logger)
    {
        _taskService = taskService;
        _shortcutService = shortcutService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> GenerateUacBypassShortcutAsync(AppPolicy policy, string shortcutSaveDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policy.FilePath) || !File.Exists(policy.FilePath))
        {
            return Result.Fail(new Error("FILE_NOT_FOUND", "The specified executable file does not exist."));
        }

        // 1. Enforce LPE Security Mitigation (World-Writable Directory Check)
        var securityCheck = CheckDirectorySecurity(Path.GetDirectoryName(policy.FilePath));
        if (!securityCheck.IsSuccess)
        {
            _logger.LogWarning("LPE Security Check Failed for {FilePath}: {Message}", policy.FilePath, securityCheck.Error?.Message);
            return Result.Fail(securityCheck.Error!);
        }

        // 2. Synthesize Scheduled Task
        var taskResult = await _taskService.CreateElevatedTaskAsync(policy, cancellationToken);
        if (!taskResult.IsSuccess)
        {
            return Result.Fail(taskResult.Error!);
        }
        string taskName = taskResult.Value;

        // 3. Synthesize .lnk Shortcut
        string safeProductName = string.IsNullOrWhiteSpace(policy.ProductName)
            ? Path.GetFileNameWithoutExtension(policy.FilePath)
            : string.Join("_", policy.ProductName.Split(Path.GetInvalidFileNameChars()));

        string shortcutPath = Path.Combine(shortcutSaveDirectory, $"{safeProductName}.lnk");

        string targetPath = "schtasks.exe";
        string arguments = $"/run /tn \"{taskName}\"";
        string iconLocation = $"{policy.FilePath}, 0"; // Force 0th icon extraction

        var shortcutResult = await _shortcutService.CreateShortcutAsync(shortcutPath, targetPath, arguments, iconLocation, cancellationToken);
        if (!shortcutResult.IsSuccess)
        {
            return Result.Fail(shortcutResult.Error!);
        }

        _logger.LogInformation("Successfully orchestrated UAC Bypass shortcut at {ShortcutPath}", shortcutPath);
        return Result.Success();
    }

    private Result CheckDirectorySecurity(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath)) return Result.Fail(new Error("INVALID_PATH", "Directory path is invalid."));

        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
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
                        $"The directory '{directoryPath}' grants write access to standard users. " +
                        "Creating a UAC bypass for this executable is blocked to prevent Local Privilege Escalation (malware could replace the file)."));
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inspect ACLs for directory {DirectoryPath}.", directoryPath);
            return Result.Fail(new Error("ACL_INSPECTION_FAILED", "Could not verify the security of the target directory."));
        }
    }
}